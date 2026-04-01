using FinRecon.Core.Common;
using FinRecon.Core.Domain.Entities;
using FinRecon.Core.Domain.Enums;
using FinRecon.Core.Domain.ValueObjects;
using FinRecon.Core.Interfaces;

namespace FinRecon.Core.Services;

public class ReconciliationEngine : IReconciliationEngine
{
    public Task<Result<(IReadOnlyList<ReconciliationRecord> Records, ReconciliationReport Report)>> RunAsync(
        Guid jobId,
        IReadOnlyList<FileRecord> incomingRecords,
        IReadOnlyList<ReconciliationRecord> previousRecords,
        CancellationToken ct = default)
    {
        // Build lookup of previous records by composite key (ClientId, ProductType)
        var previousByKey = previousRecords
            .ToDictionary(r => (r.ClientId, r.ProductType));

        var results = new List<ReconciliationRecord>(incomingRecords.Count + previousByKey.Count);
        var seenKeys = new HashSet<(string, ProductType)>();

        foreach (var incoming in incomingRecords)
        {
            var key = (incoming.ClientId, incoming.ProductType);
            seenKeys.Add(key);

            if (previousByKey.TryGetValue(key, out var previous))
            {
                var delta = Math.Abs(incoming.Value - previous.CurrentValue);
                var status = delta <= ReconciliationConstants.MatchTolerance
                    ? RecordStatus.Matched
                    : RecordStatus.Discrepant;

                results.Add(new ReconciliationRecord(
                    jobId,
                    incoming.ClientId,
                    incoming.ProductType,
                    incoming.Value,
                    previous.CurrentValue,
                    status));
            }
            else
            {
                results.Add(new ReconciliationRecord(
                    jobId,
                    incoming.ClientId,
                    incoming.ProductType,
                    incoming.Value,
                    previousValue: null,
                    RecordStatus.New));
            }
        }

        // Records present in previous but absent in current → Missing
        foreach (var (key, previous) in previousByKey)
        {
            if (!seenKeys.Contains(key))
            {
                results.Add(ReconciliationRecord.CreateMissing(
                    jobId,
                    previous.ClientId,
                    previous.ProductType,
                    previous.CurrentValue));
            }
        }

        var report = ReconciliationReport.From(jobId, results);

        return Task.FromResult(
            Result<(IReadOnlyList<ReconciliationRecord>, ReconciliationReport)>.Ok((results, report)));
    }
}
