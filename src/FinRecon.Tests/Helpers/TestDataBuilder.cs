using FinRecon.Core.Domain.Entities;
using FinRecon.Core.Domain.Enums;
using FinRecon.Core.Domain.ValueObjects;

namespace FinRecon.Tests.Helpers;

public static class TestDataBuilder
{
    private static readonly DateOnly DefaultDate = new(2025, 1, 15);

    public static ReconciliationJob CreateJob(
        string filename = "test.csv",
        string fileHash = "abc123",
        string storageKey = "2025-01-15/test.csv",
        DateOnly? referenceDate = null)
        => new(filename, fileHash, storageKey, referenceDate ?? DefaultDate);

    public static ReconciliationRecord CreateRecord(
        Guid jobId = default,
        string clientId = "C001",
        ProductType productType = ProductType.Equity,
        decimal currentValue = 1000m,
        decimal? previousValue = null,
        RecordStatus status = RecordStatus.Matched)
        => new(jobId == default ? Guid.NewGuid() : jobId, clientId, productType, currentValue, previousValue, status);

    public static FileRecord CreateFileRecord(
        string clientId = "C001",
        ProductType productType = ProductType.Equity,
        decimal value = 1000m)
        => new(clientId, productType, value);
}
