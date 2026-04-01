using FinRecon.Core.Domain.Enums;

namespace FinRecon.Core.Domain.Entities;

public class ReconciliationReport
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public int TotalRecords { get; private set; }
    public int Matched { get; private set; }
    public int Discrepant { get; private set; }
    public int NewRecords { get; private set; }
    public int MissingRecords { get; private set; }
    public decimal TotalDelta { get; private set; }
    public DateTime GeneratedAt { get; private set; }

    // Required by EF Core
    private ReconciliationReport() { }

    private ReconciliationReport(Guid jobId, int total, int matched, int discrepant, int newRecords, int missing, decimal totalDelta)
    {
        Id = Guid.NewGuid();
        JobId = jobId;
        TotalRecords = total;
        Matched = matched;
        Discrepant = discrepant;
        NewRecords = newRecords;
        MissingRecords = missing;
        TotalDelta = totalDelta;
        GeneratedAt = DateTime.UtcNow;
    }

    public static ReconciliationReport From(Guid jobId, IEnumerable<ReconciliationRecord> records)
    {
        var list = records.ToList();
        return new ReconciliationReport(
            jobId,
            total: list.Count,
            matched: list.Count(r => r.Status == RecordStatus.Matched),
            discrepant: list.Count(r => r.Status == RecordStatus.Discrepant),
            newRecords: list.Count(r => r.Status == RecordStatus.New),
            missing: list.Count(r => r.Status == RecordStatus.Missing),
            totalDelta: list.Sum(r => r.Delta ?? 0m)
        );
    }
}
