using FinRecon.Core.Interfaces;
using FinRecon.Infrastructure.Messaging.Messages;
using FinRecon.Worker.Parsers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinRecon.Worker.Consumers;

public class ReconciliationJobCreatedConsumer : IConsumer<ReconciliationJobCreated>
{
    private readonly IReconciliationRepository _repository;
    private readonly IFileStorageService _storage;
    private readonly IReconciliationEngine _engine;
    private readonly IEnumerable<IFileParser> _parsers;
    private readonly ILogger<ReconciliationJobCreatedConsumer> _logger;

    public ReconciliationJobCreatedConsumer(
        IReconciliationRepository repository,
        IFileStorageService storage,
        IReconciliationEngine engine,
        IEnumerable<IFileParser> parsers,
        ILogger<ReconciliationJobCreatedConsumer> logger)
    {
        _repository = repository;
        _storage = storage;
        _engine = engine;
        _parsers = parsers;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReconciliationJobCreated> context)
    {
        var jobId = context.Message.JobId;
        var ct = context.CancellationToken;

        _logger.LogInformation("Processing reconciliation job {JobId}", jobId);

        // 1. Load job
        var job = await _repository.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            _logger.LogError("Job {JobId} not found — skipping", jobId);
            return;
        }

        // 2. Mark as processing
        var transitionResult = job.MarkProcessing();
        if (!transitionResult.IsSuccess)
        {
            _logger.LogWarning("Job {JobId} cannot transition to Processing (current status may already be beyond pending)", jobId);
            return;
        }
        await _repository.UpdateJobAsync(job, ct);

        try
        {
            // 3. Download file from storage
            var fileStream = await _storage.DownloadAsync(job.StorageKey, ct);

            // 4. Find the right parser
            var parser = _parsers.FirstOrDefault(p => p.CanParse(job.Filename));
            if (parser is null)
            {
                _logger.LogError("No parser found for file {Filename}", job.Filename);
                job.MarkFailed();
                await _repository.UpdateJobAsync(job, ct);
                return;
            }

            // 5. Parse file
            var parseResult = await parser.ParseAsync(fileStream, ct);
            if (!parseResult.IsSuccess)
            {
                _logger.LogError("Failed to parse file {Filename}: {Error}", job.Filename, parseResult.Error?.Message);
                job.MarkFailed();
                await _repository.UpdateJobAsync(job, ct);
                return;
            }

            // 6. Load previous records (most recent completed job before this date)
            var previousJob = await _repository.GetMostRecentCompletedBeforeDateAsync(job.ReferenceDate, ct);
            var previousRecords = previousJob is not null
                ? await _repository.GetRecordsByJobIdAsync(previousJob.Id, ct: ct)
                : Array.Empty<Core.Domain.Entities.ReconciliationRecord>();

            // 7. Run reconciliation engine
            var engineResult = await _engine.RunAsync(jobId, parseResult.Value!, previousRecords, ct);
            if (!engineResult.IsSuccess)
            {
                _logger.LogError("Reconciliation engine failed for job {JobId}: {Error}", jobId, engineResult.Error?.Message);
                job.MarkFailed();
                await _repository.UpdateJobAsync(job, ct);
                return;
            }

            // 8. Persist records and report
            await _repository.AddRecordsAsync(engineResult.Value.Records, ct);
            await _repository.AddReportAsync(engineResult.Value.Report, ct);

            // 9. Mark completed
            job.MarkCompleted();
            await _repository.UpdateJobAsync(job, ct);

            _logger.LogInformation(
                "Job {JobId} completed: {Total} records, {Matched} matched, {Discrepant} discrepant, {New} new, {Missing} missing",
                jobId,
                engineResult.Value.Report.TotalRecords,
                engineResult.Value.Report.Matched,
                engineResult.Value.Report.Discrepant,
                engineResult.Value.Report.NewRecords,
                engineResult.Value.Report.MissingRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing job {JobId}", jobId);
            job.MarkFailed();
            await _repository.UpdateJobAsync(job, ct);
            throw; // Re-throw so MassTransit retry policy kicks in
        }
    }
}
