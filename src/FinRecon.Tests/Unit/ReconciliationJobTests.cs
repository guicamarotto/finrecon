using FluentAssertions;
using FinRecon.Core.Domain.Enums;
using FinRecon.Tests.Helpers;

namespace FinRecon.Tests.Unit;

public class ReconciliationJobTests
{
    [Fact]
    public void MarkProcessing_WhenStatusIsPending_TransitionsToProcessing()
    {
        var job = TestDataBuilder.CreateJob();
        job.Status.Should().Be(JobStatus.Pending);

        var result = job.MarkProcessing();

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Processing);
    }

    [Fact]
    public void MarkCompleted_WhenStatusIsProcessing_TransitionsToCompletedAndSetsCompletedAt()
    {
        var job = TestDataBuilder.CreateJob();
        job.MarkProcessing();

        var result = job.MarkCompleted();

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Completed);
        job.CompletedAt.Should().NotBeNull();
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkFailed_WhenStatusIsProcessing_TransitionsToFailed()
    {
        var job = TestDataBuilder.CreateJob();
        job.MarkProcessing();

        var result = job.MarkFailed();

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Failed);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_WhenStatusIsPending_TransitionsToFailed()
    {
        var job = TestDataBuilder.CreateJob();

        var result = job.MarkFailed();

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Failed);
    }

    [Fact]
    public void MarkProcessing_WhenAlreadyCompleted_ReturnsFailure()
    {
        var job = TestDataBuilder.CreateJob();
        job.MarkProcessing();
        job.MarkCompleted();

        var result = job.MarkProcessing();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("job.invalid_transition");
    }

    [Fact]
    public void MarkCompleted_WhenStatusIsPending_ReturnsFailure()
    {
        var job = TestDataBuilder.CreateJob();

        var result = job.MarkCompleted();

        result.IsSuccess.Should().BeFalse();
        job.Status.Should().Be(JobStatus.Pending);
    }

    [Fact]
    public void NewJob_HasPendingStatus_And_NoCompletedAt()
    {
        var job = TestDataBuilder.CreateJob();

        job.Status.Should().Be(JobStatus.Pending);
        job.CompletedAt.Should().BeNull();
        job.Id.Should().NotBeEmpty();
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
