using FluentAssertions;
using FinRecon.Core.Domain.Enums;
using FinRecon.Infrastructure.Persistence.Repositories;
using FinRecon.Tests.Fixtures;
using FinRecon.Tests.Helpers;

namespace FinRecon.Tests.Integration;

[Collection("postgres")]
public class ReconciliationRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public ReconciliationRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExistsByHashAndDate_ReturnsFalse_WhenNoDuplicateExists()
    {
        await using var db = _fixture.CreateDbContext();
        var repo = new ReconciliationRepository(db);

        var exists = await repo.ExistsByHashAndDateAsync("nonexistent_hash", new DateOnly(2024, 6, 1));

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByHashAndDate_ReturnsTrue_WhenSameHashAndDateExist()
    {
        await using var db = _fixture.CreateDbContext();
        var repo = new ReconciliationRepository(db);

        var job = TestDataBuilder.CreateJob(
            fileHash: "unique_test_hash_001",
            referenceDate: new DateOnly(2024, 6, 1));
        await repo.AddJobAsync(job);

        var exists = await repo.ExistsByHashAndDateAsync("unique_test_hash_001", new DateOnly(2024, 6, 1));

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByHashAndDate_ReturnsFalse_WhenSameHashButDifferentDate()
    {
        await using var db = _fixture.CreateDbContext();
        var repo = new ReconciliationRepository(db);

        var job = TestDataBuilder.CreateJob(
            fileHash: "unique_test_hash_002",
            referenceDate: new DateOnly(2024, 7, 1));
        await repo.AddJobAsync(job);

        // Same hash, different date → not a duplicate
        var exists = await repo.ExistsByHashAndDateAsync("unique_test_hash_002", new DateOnly(2024, 7, 2));

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetMostRecentCompleted_ReturnsMostRecentJobBeforeGivenDate()
    {
        await using var db = _fixture.CreateDbContext();
        var repo = new ReconciliationRepository(db);

        // Three completed jobs on different dates
        var job1 = TestDataBuilder.CreateJob(fileHash: "hash_jan01", referenceDate: new DateOnly(2024, 1, 1));
        var job2 = TestDataBuilder.CreateJob(fileHash: "hash_jan15", referenceDate: new DateOnly(2024, 1, 15));
        var job3 = TestDataBuilder.CreateJob(fileHash: "hash_feb01", referenceDate: new DateOnly(2024, 2, 1));

        foreach (var j in new[] { job1, job2, job3 })
        {
            j.MarkProcessing();
            j.MarkCompleted();
            await repo.AddJobAsync(j);
        }

        // Query for most recent before Feb 1 → should be Jan 15
        var result = await repo.GetMostRecentCompletedBeforeDateAsync(new DateOnly(2024, 2, 1));

        result.Should().NotBeNull();
        result!.ReferenceDate.Should().Be(new DateOnly(2024, 1, 15));
    }

    [Fact]
    public async Task GetPaged_RespectsStatusFilter()
    {
        await using var db = _fixture.CreateDbContext();
        var repo = new ReconciliationRepository(db);

        var pending = TestDataBuilder.CreateJob(fileHash: "status_filter_pending");
        var completed = TestDataBuilder.CreateJob(fileHash: "status_filter_completed");
        completed.MarkProcessing();
        completed.MarkCompleted();

        await repo.AddJobAsync(pending);
        await repo.AddJobAsync(completed);

        var pendingResult = await repo.GetPagedAsync(1, 50, JobStatus.Pending);
        var completedResult = await repo.GetPagedAsync(1, 50, JobStatus.Completed);

        pendingResult.Items.Should().Contain(j => j.FileHash == "status_filter_pending");
        completedResult.Items.Should().Contain(j => j.FileHash == "status_filter_completed");
        pendingResult.Items.Should().NotContain(j => j.FileHash == "status_filter_completed");
    }
}
