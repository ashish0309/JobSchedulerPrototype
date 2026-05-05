using Microsoft.Extensions.Options;

namespace JobSchedulerPrototype.Jobs;

public sealed class QueuedJobWorker
{
    private readonly IJobLifecycleService _lifecycle;
    private readonly IJobDispatcher _dispatcher;
    private readonly ILogger<QueuedJobWorker> _logger;
    private readonly TimeSpan _simulatedWorkDuration;

    public QueuedJobWorker(
        IJobLifecycleService lifecycle,
        IJobDispatcher dispatcher,
        ILogger<QueuedJobWorker> logger,
        IOptions<JobWorkerOptions> options)
        : this(
            lifecycle,
            dispatcher,
            logger,
            options.Value.ValidSimulatedWorkDuration)
    {
    }

    public QueuedJobWorker(
        IJobLifecycleService lifecycle,
        IJobDispatcher dispatcher,
        ILogger<QueuedJobWorker> logger,
        TimeSpan simulatedWorkDuration)
    {
        _lifecycle = lifecycle;
        _dispatcher = dispatcher;
        _logger = logger;
        _simulatedWorkDuration = simulatedWorkDuration;
    }

    public async Task<bool> ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        var job = _lifecycle.ClaimNextDueJob(DateTimeOffset.UtcNow);
        if (job is null)
        {
            return false;
        }

        _logger.LogInformation(
            "Claimed job {JobId} type={JobType} attempt={AttemptNumber}",
            job.Id,
            job.Type,
            job.AttemptCount);

        await Task.Delay(_simulatedWorkDuration, cancellationToken);
        var result = await _dispatcher.ExecuteAsync(job, cancellationToken);
        var completion = _lifecycle.CompleteExecution(job, result);

        if (completion.Status == JobExecutionCompletionStatus.Completed)
        {
            _logger.LogInformation(
                "Completed job {JobId} type={JobType} attempt={AttemptNumber}",
                job.Id,
                job.Type,
                job.AttemptCount);
        }
        else if (completion.Status == JobExecutionCompletionStatus.RetryScheduled)
        {
            _logger.LogWarning(
                "Failed job {JobId} type={JobType} attempt={AttemptNumber} reason={FailureReason}; scheduled retry attempt={NextAttemptNumber} runAt={ScheduledAt}",
                job.Id,
                job.Type,
                job.AttemptCount,
                completion.FailureReason,
                job.AttemptCount + 1,
                completion.RetryScheduledAt);
        }
        else
        {
            _logger.LogError(
                "Failed job {JobId} type={JobType} finalAttempt={AttemptNumber} reason={FailureReason}",
                job.Id,
                job.Type,
                job.AttemptCount,
                completion.FailureReason);
        }

        return true;
    }
}
