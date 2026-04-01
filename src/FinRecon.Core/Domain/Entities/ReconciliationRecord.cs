using FinRecon.Core.Domain.Enums;

namespace FinRecon.Core.Domain.Entities;

public class ReconciliationRecord
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public string ClientId { get; private set; }
    public ProductType ProductType { get; private set; }
    public decimal CurrentValue { get; private set; }
    public decimal? PreviousValue { get; private set; }
    public decimal? Delta { get; private set; }
    public RecordStatus Status { get; private set; }

    // Required by EF Core
    private ReconciliationRecord() { ClientId = string.Empty; }

    public ReconciliationRecord(
        Guid jobId,
        string clientId,
        ProductType productType,
        decimal currentValue,
        decimal? previousValue,
        RecordStatus status)
    {
        Id = Guid.NewGuid();
        JobId = jobId;
        ClientId = clientId;
        ProductType = productType;
        CurrentValue = currentValue;
        PreviousValue = previousValue;
        Delta = previousValue.HasValue ? currentValue - previousValue.Value : null;
        Status = status;
    }

    // Used for missing records (not in current file, existed in previous)
    public static ReconciliationRecord CreateMissing(Guid jobId, string clientId, ProductType productType, decimal previousValue)
    {
        return new ReconciliationRecord(jobId, clientId, productType, 0m, previousValue, RecordStatus.Missing)
        {
            Delta = -previousValue
        };
    }

    // EF Core materialisation constructor for missing records requires Delta override
    private ReconciliationRecord(
        Guid jobId,
        string clientId,
        ProductType productType,
        decimal currentValue,
        decimal? previousValue,
        RecordStatus status,
        decimal? deltaOverride) : this(jobId, clientId, productType, currentValue, previousValue, status)
    {
        Delta = deltaOverride;
    }
}
