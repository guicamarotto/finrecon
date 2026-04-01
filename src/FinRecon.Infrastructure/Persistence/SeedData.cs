using FinRecon.Core.Domain.Entities;
using FinRecon.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinRecon.Infrastructure.Persistence;

/// <summary>
/// Deterministic seed data so that docker compose up produces a usable demo immediately.
/// Uses hardcoded GUIDs to ensure idempotent migrations.
/// </summary>
internal static class SeedData
{
    private static readonly Guid CompletedJobId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ReportId       = new("33333333-3333-3333-3333-333333333333");
    private static readonly DateOnly Date1 = new(2025, 1, 14);

    public static void Apply(ModelBuilder modelBuilder)
    {
        SeedJobs(modelBuilder);
        SeedRecords(modelBuilder);
        SeedReport(modelBuilder);
    }

    private static void SeedJobs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReconciliationJob>().HasData(
            new
            {
                Id          = CompletedJobId,
                Filename    = "portfolio_2025-01-14.csv",
                FileHash    = "deadbeefdeadbeefdeadbeefdeadbeef01010101010101010101010101010101",
                StorageKey  = "2025-01-14/11111111-1111-1111-1111-111111111111/portfolio_2025-01-14.csv",
                Status      = JobStatus.Completed,
                ReferenceDate = Date1,
                CreatedAt   = new DateTime(2025, 1, 14, 9, 0, 0, DateTimeKind.Utc),
                CompletedAt = (DateTime?)new DateTime(2025, 1, 14, 9, 0, 5, DateTimeKind.Utc)
            }
        );
    }

    private static void SeedRecords(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReconciliationRecord>().HasData(
            new
            {
                Id            = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                JobId         = CompletedJobId,
                ClientId      = "C001",
                ProductType   = ProductType.Equity,
                CurrentValue  = 15000.00m,
                PreviousValue = (decimal?)null,
                Delta         = (decimal?)null,
                Status        = RecordStatus.New
            },
            new
            {
                Id            = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                JobId         = CompletedJobId,
                ClientId      = "C002",
                ProductType   = ProductType.Crypto,
                CurrentValue  = 3200.50m,
                PreviousValue = (decimal?)null,
                Delta         = (decimal?)null,
                Status        = RecordStatus.New
            },
            new
            {
                Id            = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                JobId         = CompletedJobId,
                ClientId      = "C003",
                ProductType   = ProductType.Fund,
                CurrentValue  = 8750.00m,
                PreviousValue = (decimal?)null,
                Delta         = (decimal?)null,
                Status        = RecordStatus.New
            },
            new
            {
                Id            = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                JobId         = CompletedJobId,
                ClientId      = "C004",
                ProductType   = ProductType.Bond,
                CurrentValue  = 50000.00m,
                PreviousValue = (decimal?)null,
                Delta         = (decimal?)null,
                Status        = RecordStatus.New
            }
        );
    }

    private static void SeedReport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReconciliationReport>().HasData(new
        {
            Id             = ReportId,
            JobId          = CompletedJobId,
            TotalRecords   = 4,
            Matched        = 0,
            Discrepant     = 0,
            NewRecords     = 4,
            MissingRecords = 0,
            TotalDelta     = 0m,
            GeneratedAt    = new DateTime(2025, 1, 14, 9, 0, 5, DateTimeKind.Utc)
        });
    }
}
