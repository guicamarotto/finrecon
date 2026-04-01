using FinRecon.Core.Common;
using FinRecon.Core.Domain.Entities;
using FinRecon.Core.Domain.ValueObjects;

namespace FinRecon.Core.Interfaces;

public interface IReconciliationEngine
{
    Task<Result<(IReadOnlyList<ReconciliationRecord> Records, ReconciliationReport Report)>> RunAsync(
        Guid jobId,
        IReadOnlyList<FileRecord> incomingRecords,
        IReadOnlyList<ReconciliationRecord> previousRecords,
        CancellationToken ct = default);
}
