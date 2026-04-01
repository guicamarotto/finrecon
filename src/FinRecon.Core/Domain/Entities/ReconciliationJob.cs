using FinRecon.Core.Common;
using FinRecon.Core.Domain.Enums;
using FinRecon.Core.Domain.Errors;

namespace FinRecon.Core.Domain.Entities;

public class ReconciliationJob
{
    public Guid Id { get; private set; }
    public string Filename { get; private set; }
    public string FileHash { get; private set; }
    public string StorageKey { get; private set; }
    public JobStatus Status { get; private set; }
    public DateOnly ReferenceDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Required by EF Core
    private ReconciliationJob() { Filename = string.Empty; FileHash = string.Empty; StorageKey = string.Empty; }

    public ReconciliationJob(string filename, string fileHash, string storageKey, DateOnly referenceDate)
    {
        Id = Guid.NewGuid();
        Filename = filename;
        FileHash = fileHash;
        StorageKey = storageKey;
        ReferenceDate = referenceDate;
        Status = JobStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Result MarkProcessing()
    {
        if (Status != JobStatus.Pending)
            return Result.Fail(DomainErrors.Job.InvalidTransition);

        Status = JobStatus.Processing;
        return Result.Ok();
    }

    public Result MarkCompleted()
    {
        if (Status != JobStatus.Processing)
            return Result.Fail(DomainErrors.Job.InvalidTransition);

        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result MarkFailed()
    {
        if (Status != JobStatus.Processing && Status != JobStatus.Pending)
            return Result.Fail(DomainErrors.Job.InvalidTransition);

        Status = JobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        return Result.Ok();
    }
}
