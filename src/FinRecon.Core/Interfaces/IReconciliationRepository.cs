using FinRecon.Core.Common;
using FinRecon.Core.Domain.Entities;
using FinRecon.Core.Domain.Enums;

namespace FinRecon.Core.Interfaces;

public interface IReconciliationRepository
{
    Task<ReconciliationJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByHashAndDateAsync(string hash, DateOnly referenceDate, CancellationToken ct = default);
    Task<ReconciliationJob?> GetMostRecentCompletedBeforeDateAsync(DateOnly date, CancellationToken ct = default);
    Task<PagedResult<ReconciliationJob>> GetPagedAsync(int page, int pageSize, JobStatus? status = null, CancellationToken ct = default);
    Task<IReadOnlyList<ReconciliationRecord>> GetRecordsByJobIdAsync(Guid jobId, RecordStatus? status = null, ProductType? productType = null, CancellationToken ct = default);
    Task<ReconciliationReport?> GetReportByJobIdAsync(Guid jobId, CancellationToken ct = default);
    Task AddJobAsync(ReconciliationJob job, CancellationToken ct = default);
    Task AddRecordsAsync(IEnumerable<ReconciliationRecord> records, CancellationToken ct = default);
    Task AddReportAsync(ReconciliationReport report, CancellationToken ct = default);
    Task UpdateJobAsync(ReconciliationJob job, CancellationToken ct = default);
}
