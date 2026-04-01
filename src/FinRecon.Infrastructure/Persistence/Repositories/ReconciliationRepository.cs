using FinRecon.Core.Common;
using FinRecon.Core.Domain.Entities;
using FinRecon.Core.Domain.Enums;
using FinRecon.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinRecon.Infrastructure.Persistence.Repositories;

public class ReconciliationRepository : IReconciliationRepository
{
    private readonly FinReconDbContext _db;

    public ReconciliationRepository(FinReconDbContext db) => _db = db;

    public Task<ReconciliationJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ReconciliationJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<bool> ExistsByHashAndDateAsync(string hash, DateOnly referenceDate, CancellationToken ct = default)
        => _db.ReconciliationJobs.AnyAsync(j => j.FileHash == hash && j.ReferenceDate == referenceDate, ct);

    public Task<ReconciliationJob?> GetMostRecentCompletedBeforeDateAsync(DateOnly date, CancellationToken ct = default)
        => _db.ReconciliationJobs
            .Where(j => j.Status == JobStatus.Completed && j.ReferenceDate < date)
            .OrderByDescending(j => j.ReferenceDate)
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<ReconciliationJob>> GetPagedAsync(
        int page, int pageSize, JobStatus? status = null, CancellationToken ct = default)
    {
        var query = _db.ReconciliationJobs.AsQueryable();

        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ReconciliationJob>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<ReconciliationRecord>> GetRecordsByJobIdAsync(
        Guid jobId, RecordStatus? status = null, ProductType? productType = null, CancellationToken ct = default)
    {
        var query = _db.ReconciliationRecords.Where(r => r.JobId == jobId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (productType.HasValue)
            query = query.Where(r => r.ProductType == productType.Value);

        return await query.OrderBy(r => r.ClientId).ToListAsync(ct);
    }

    public Task<ReconciliationReport?> GetReportByJobIdAsync(Guid jobId, CancellationToken ct = default)
        => _db.ReconciliationReports.FirstOrDefaultAsync(r => r.JobId == jobId, ct);

    public async Task AddJobAsync(ReconciliationJob job, CancellationToken ct = default)
    {
        await _db.ReconciliationJobs.AddAsync(job, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRecordsAsync(IEnumerable<ReconciliationRecord> records, CancellationToken ct = default)
    {
        await _db.ReconciliationRecords.AddRangeAsync(records, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddReportAsync(ReconciliationReport report, CancellationToken ct = default)
    {
        await _db.ReconciliationReports.AddAsync(report, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateJobAsync(ReconciliationJob job, CancellationToken ct = default)
    {
        _db.ReconciliationJobs.Update(job);
        await _db.SaveChangesAsync(ct);
    }
}
