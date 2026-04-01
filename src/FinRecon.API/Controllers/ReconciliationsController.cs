using System.Security.Cryptography;
using FinRecon.API.DTOs;
using FinRecon.API.Mapping;
using FinRecon.Core.Domain.Entities;
using FinRecon.Core.Domain.Enums;
using FinRecon.Core.Interfaces;
using FinRecon.Infrastructure.Messaging.Messages;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinRecon.API.Controllers;

[ApiController]
[Route("api/reconciliations")]
[Authorize]
public class ReconciliationsController : ControllerBase
{
    private readonly IReconciliationRepository _repository;
    private readonly IFileStorageService _storage;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<ReconciliationsController> _logger;

    public ReconciliationsController(
        IReconciliationRepository repository,
        IFileStorageService storage,
        IPublishEndpoint publisher,
        ILogger<ReconciliationsController> logger)
    {
        _repository = repository;
        _storage = storage;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>Upload a financial product file for reconciliation.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateReconciliationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadReconciliationRequest request,
        CancellationToken ct)
    {
        using var stream = request.File.OpenReadStream();

        // Compute SHA-256 hash for duplicate detection
        var hash = await ComputeSha256Async(stream, ct);
        stream.Position = 0;

        // Check for duplicate file on same date
        if (await _repository.ExistsByHashAndDateAsync(hash, request.ReferenceDate, ct))
        {
            return Conflict(new ErrorResponse(
                "job.duplicate_file",
                "A file with this content already exists for the given reference date."));
        }

        // Store file
        var storageKey = await _storage.UploadAsync(
            request.File.FileName, stream, request.File.ContentType, ct);

        // Create job
        var job = new ReconciliationJob(request.File.FileName, hash, storageKey, request.ReferenceDate);
        await _repository.AddJobAsync(job, ct);

        // Publish async processing event
        await _publisher.Publish(new ReconciliationJobCreated(job.Id), ct);

        _logger.LogInformation("Created reconciliation job {JobId} for date {Date}", job.Id, request.ReferenceDate);

        return CreatedAtAction(
            nameof(GetById),
            new { jobId = job.Id },
            new CreateReconciliationResponse(job.Id, job.Status.ToString()));
    }

    /// <summary>List reconciliation jobs with optional status filter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReconciliationJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        JobStatus? jobStatus = null;
        if (status != null && Enum.TryParse<JobStatus>(status, ignoreCase: true, out var parsed))
            jobStatus = parsed;

        var result = await _repository.GetPagedAsync(page, pageSize, jobStatus, ct);
        return Ok(result.ToDto(j => j.ToDto()));
    }

    /// <summary>Get a reconciliation job with its report.</summary>
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(ReconciliationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid jobId, CancellationToken ct)
    {
        var job = await _repository.GetByIdAsync(jobId, ct);
        if (job is null)
            return NotFound(new ErrorResponse("job.not_found", "Reconciliation job not found."));

        var report = await _repository.GetReportByJobIdAsync(jobId, ct);

        return Ok(new ReconciliationDetailDto(job.ToDto(), report?.ToDto()));
    }

    /// <summary>Get records for a reconciliation job with optional filters.</summary>
    [HttpGet("{jobId:guid}/records")]
    [ProducesResponseType(typeof(IReadOnlyList<ReconciliationRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecords(
        Guid jobId,
        [FromQuery] string? status = null,
        [FromQuery] string? productType = null,
        CancellationToken ct = default)
    {
        RecordStatus? recordStatus = null;
        if (status != null && Enum.TryParse<RecordStatus>(status, ignoreCase: true, out var parsedStatus))
            recordStatus = parsedStatus;

        ProductType? product = null;
        if (productType != null && Enum.TryParse<ProductType>(productType, ignoreCase: true, out var parsedProduct))
            product = parsedProduct;

        var records = await _repository.GetRecordsByJobIdAsync(jobId, recordStatus, product, ct);
        return Ok(records.Select(r => r.ToDto()));
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
