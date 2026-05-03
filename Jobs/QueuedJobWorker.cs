namespace JobSchedulerPrototype.Jobs;

public sealed class QueuedJobWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SimulatedWorkDuration = TimeSpan.FromSeconds(2);

    private readonly IJobStore _jobs;

    public QueuedJobWorker(IJobStore jobs)
    {
        _jobs = jobs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = _jobs.TryClaimNextQueuedJob();
            if (job is null)
            {
                await Task.Delay(PollInterval, stoppingToken);
                continue;
            }

            await Task.Delay(SimulatedWorkDuration, stoppingToken);
            _jobs.MarkCompleted(job.Id);
        }
    }
}
