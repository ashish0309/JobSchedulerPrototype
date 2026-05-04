namespace JobSchedulerPrototype.Jobs;

public sealed class QueuedJobWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SimulatedWorkDuration = TimeSpan.FromSeconds(2);

    private readonly IJobStore _jobs;
    private readonly IJobDispatcher _dispatcher;
    private readonly IJobDefinitionRegistry _definitions;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _simulatedWorkDuration;

    public QueuedJobWorker(
        IJobStore jobs,
        IJobDispatcher dispatcher,
        IJobDefinitionRegistry definitions)
        : this(jobs, dispatcher, definitions, PollInterval, SimulatedWorkDuration)
    {
    }

    public QueuedJobWorker(
        IJobStore jobs,
        IJobDispatcher dispatcher,
        IJobDefinitionRegistry definitions,
        TimeSpan pollInterval,
        TimeSpan simulatedWorkDuration)
    {
        _jobs = jobs;
        _dispatcher = dispatcher;
        _definitions = definitions;
        _pollInterval = pollInterval;
        _simulatedWorkDuration = simulatedWorkDuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processedJob = await ProcessNextJobAsync(stoppingToken);
            if (!processedJob)
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    public async Task<bool> ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        var job = _jobs.TryClaimNextDueJob(DateTimeOffset.UtcNow);
        if (job is null)
        {
            return false;
        }

        await Task.Delay(_simulatedWorkDuration, cancellationToken);
        var result = await _dispatcher.ExecuteAsync(job, cancellationToken);

        if (result.Succeeded)
        {
            _jobs.MarkCompleted(job.Id);
        }
        else if (job.AttemptCount < job.MaxAttempts
            && _definitions.Find(job.Type) is { } definition)
        {
            var scheduledAt = DateTimeOffset.UtcNow.AddSeconds(definition.RetryDelaySeconds);
            _jobs.ScheduleRetry(
                job.Id,
                result.FailureReason ?? "Job execution failed.",
                scheduledAt);
        }
        else
        {
            _jobs.MarkFailed(job.Id, result.FailureReason ?? "Job execution failed.");
        }

        return true;
    }
}
