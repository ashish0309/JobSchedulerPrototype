using System.Text.Json;
using JobSchedulerPrototype.Jobs;
using JobSchedulerPrototype.Pages;

namespace JobSchedulerPrototype.Tests.Pages;

public sealed class IndexModelTests
{
    [Fact]
    public void OnGetLoadsJobsAndStatusCounts()
    {
        var store = new InMemoryJobStore();
        var completedJob = CreateJob(enqueuedAt: new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero));
        var failedJob = CreateJob(enqueuedAt: new DateTimeOffset(2026, 5, 4, 10, 1, 0, TimeSpan.Zero));
        var queuedJob = CreateJob(enqueuedAt: new DateTimeOffset(2026, 5, 4, 10, 2, 0, TimeSpan.Zero));
        store.Add(queuedJob);
        store.Add(completedJob);
        store.Add(failedJob);
        store.TryClaimNextQueuedJob();
        store.MarkCompleted(completedJob.Id);
        store.TryClaimNextQueuedJob();
        store.MarkFailed(failedJob.Id, "SMTP server unavailable.");
        var model = new IndexModel(store);

        model.OnGet();

        Assert.Equal(3, model.Jobs.Count);
        Assert.Equal(1, model.QueuedCount);
        Assert.Equal(0, model.RunningCount);
        Assert.Equal(1, model.CompletedCount);
        Assert.Equal(1, model.FailedCount);

        var failedSummary = model.Jobs.Single(job => job.Id == failedJob.Id);
        Assert.Equal(JobStatus.Failed, failedSummary.Status);
        Assert.Equal("SMTP server unavailable.", failedSummary.FailureReason);
        Assert.Equal($"/api/jobs/{failedJob.Id}", failedSummary.StatusUrl);
    }

    private static JobRecord CreateJob(DateTimeOffset enqueuedAt)
    {
        return JobRecord.Enqueue(
            Guid.NewGuid(),
            "send-welcome-email",
            Payload(),
            enqueuedAt);
    }

    private static JsonElement Payload()
    {
        using var document = JsonDocument.Parse("""{"userId":"user_123"}""");
        return document.RootElement.Clone();
    }
}
