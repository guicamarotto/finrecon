using FluentAssertions;
using FinRecon.Core.Domain.Enums;
using FinRecon.Core.Domain.ValueObjects;
using FinRecon.Core.Services;
using FinRecon.Tests.Helpers;

namespace FinRecon.Tests.Unit;

public class ReconciliationEngineTests
{
    private readonly ReconciliationEngine _engine = new();
    private readonly Guid _jobId = Guid.NewGuid();

    // ── Matched ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenCurrentMatchesPreviousExactly_ReturnsMatchedStatus()
    {
        var incoming = new[] { TestDataBuilder.CreateFileRecord(value: 1000m) };
        var previous = new[] { TestDataBuilder.CreateRecord(currentValue: 1000m) };

        var result = await _engine.RunAsync(_jobId, incoming, previous);

        result.IsSuccess.Should().BeTrue();
        result.Value.Records.Should().HaveCount(1);
        result.Value.Records[0].Status.Should().Be(RecordStatus.Matched);
    }

    [Theory]
    [InlineData(1000.00, 1000.009)] // delta = 0.009 < tolerance → matched
    [InlineData(1000.00, 999.991)]  // delta = 0.009 < tolerance → matched
    [InlineData(1000.00, 1000.01)]  // delta exactly at tolerance → matched
    public async Task Run_WhenDeltaIsWithinTolerance_ReturnsMatchedStatus(decimal previous, decimal current)
    {
        var incoming = new[] { TestDataBuilder.CreateFileRecord(value: current) };
        var previousRecords = new[] { TestDataBuilder.CreateRecord(currentValue: previous) };

        var result = await _engine.RunAsync(_jobId, incoming, previousRecords);

        result.Value.Records[0].Status.Should().Be(RecordStatus.Matched);
    }

    // ── Discrepant ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1000.00, 1000.011)] // delta = 0.011 > tolerance → discrepant
    [InlineData(1000.00, 500.00)]   // large delta → discrepant
    [InlineData(1000.00, 0.00)]     // zero value → discrepant
    public async Task Run_WhenDeltaExceedsTolerance_ReturnsDiscrepantStatus(decimal previous, decimal current)
    {
        var incoming = new[] { TestDataBuilder.CreateFileRecord(value: current) };
        var previousRecords = new[] { TestDataBuilder.CreateRecord(currentValue: previous) };

        var result = await _engine.RunAsync(_jobId, incoming, previousRecords);

        result.Value.Records[0].Status.Should().Be(RecordStatus.Discrepant);
    }

    [Fact]
    public async Task Run_WhenDiscrepant_DeltaIsCurrentMinusPrevious()
    {
        var incoming = new[] { TestDataBuilder.CreateFileRecord(value: 1500m) };
        var previous = new[] { TestDataBuilder.CreateRecord(currentValue: 1000m) };

        var result = await _engine.RunAsync(_jobId, incoming, previous);

        result.Value.Records[0].Delta.Should().Be(500m);
    }

    // ── New records ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenNoPreviousRecordExists_ReturnsNewStatus()
    {
        var incoming = new[] { TestDataBuilder.CreateFileRecord(clientId: "NEW_CLIENT") };
        var previous = Array.Empty<Core.Domain.Entities.ReconciliationRecord>();

        var result = await _engine.RunAsync(_jobId, incoming, previous);

        result.Value.Records[0].Status.Should().Be(RecordStatus.New);
        result.Value.Records[0].PreviousValue.Should().BeNull();
        result.Value.Records[0].Delta.Should().BeNull();
    }

    [Fact]
    public async Task Run_WithNoPreviousJob_AllRecordsAreNew()
    {
        var incoming = new[]
        {
            TestDataBuilder.CreateFileRecord("C001", ProductType.Equity, 1000m),
            TestDataBuilder.CreateFileRecord("C002", ProductType.Crypto, 500m),
        };

        var result = await _engine.RunAsync(_jobId, incoming, Array.Empty<Core.Domain.Entities.ReconciliationRecord>());

        result.Value.Records.Should().HaveCount(2);
        result.Value.Records.Should().AllSatisfy(r => r.Status.Should().Be(RecordStatus.New));
    }

    // ── Missing records ────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenPreviousRecordAbsentInCurrent_ReturnsMissingRecord()
    {
        var incoming = Array.Empty<FileRecord>();
        var previous = new[] { TestDataBuilder.CreateRecord(clientId: "GONE", currentValue: 800m) };

        var result = await _engine.RunAsync(_jobId, incoming, previous);

        result.Value.Records.Should().HaveCount(1);
        result.Value.Records[0].Status.Should().Be(RecordStatus.Missing);
        result.Value.Records[0].ClientId.Should().Be("GONE");
    }

    // ── Composite key ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_ComparesBy_ClientId_AND_ProductType_NotJustClientId()
    {
        // Same clientId but different ProductType → two separate records
        var incoming = new[]
        {
            TestDataBuilder.CreateFileRecord("C001", ProductType.Equity, 1000m),
            TestDataBuilder.CreateFileRecord("C001", ProductType.Crypto, 500m),
        };
        var previous = new[]
        {
            TestDataBuilder.CreateRecord(clientId: "C001", productType: ProductType.Equity, currentValue: 1000m),
            // C001/Crypto is absent in previous → New
        };

        var result = await _engine.RunAsync(_jobId, incoming, previous);

        result.Value.Records.Should().HaveCount(2);
        result.Value.Records.First(r => r.ProductType == ProductType.Equity).Status.Should().Be(RecordStatus.Matched);
        result.Value.Records.First(r => r.ProductType == ProductType.Crypto).Status.Should().Be(RecordStatus.New);
    }

    // ── Report aggregation ────────────────────────────────────────────────────

    [Fact]
    public async Task Run_ReportCounts_AreCorrect()
    {
        var incoming = new[]
        {
            TestDataBuilder.CreateFileRecord("C001", ProductType.Equity, 1000m),  // matched
            TestDataBuilder.CreateFileRecord("C002", ProductType.Fund, 999m),     // discrepant (prev=500)
            TestDataBuilder.CreateFileRecord("C003", ProductType.Crypto, 200m),   // new
        };
        var previous = new[]
        {
            TestDataBuilder.CreateRecord(clientId: "C001", productType: ProductType.Equity, currentValue: 1000m),
            TestDataBuilder.CreateRecord(clientId: "C002", productType: ProductType.Fund, currentValue: 500m),
            TestDataBuilder.CreateRecord(clientId: "C004", productType: ProductType.Bond, currentValue: 100m), // missing
        };

        var result = await _engine.RunAsync(_jobId, incoming, previous);
        var report = result.Value.Report;

        report.TotalRecords.Should().Be(4);
        report.Matched.Should().Be(1);
        report.Discrepant.Should().Be(1);
        report.NewRecords.Should().Be(1);
        report.MissingRecords.Should().Be(1);
    }

    [Fact]
    public async Task Run_ReportTotalDelta_IsCorrectSumOfAllDeltas()
    {
        // C001: matched (delta=0), C002: discrepant (delta=+499)
        var incoming = new[]
        {
            TestDataBuilder.CreateFileRecord("C001", ProductType.Equity, 1000m),
            TestDataBuilder.CreateFileRecord("C002", ProductType.Fund, 999m),
        };
        var previous = new[]
        {
            TestDataBuilder.CreateRecord(clientId: "C001", productType: ProductType.Equity, currentValue: 1000m),
            TestDataBuilder.CreateRecord(clientId: "C002", productType: ProductType.Fund, currentValue: 500m),
        };

        var result = await _engine.RunAsync(_jobId, incoming, previous);

        // 0 (matched) + 499 (discrepant 999-500)
        result.Value.Report.TotalDelta.Should().Be(499m);
    }
}
