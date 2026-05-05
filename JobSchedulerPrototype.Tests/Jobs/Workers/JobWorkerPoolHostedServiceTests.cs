using System.Text.Json;
using JobSchedulerPrototype.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JobSchedulerPrototype.Tests.Jobs;

public sealed class JobWorkerPoolHostedServiceTests
{
    [Fact]
    public async Task WorkerPoolProcessesJobsConcurrentlyWhenMultipleWorkersAreConfigured()
    {
        var store = new InMemoryJobStore();
        var dispatcher = new TrackingJobDispatcher(expectedExecutions: 6);
        var options = Options.Create(new JobWorkerOptions
        {
            WorkerCount = 3,
            PollIntervalSeconds = 0,
            SimulatedWorkDurationSeconds = 0
        });
        var worker = new QueuedJobWorker(
            LifecycleService(store),
            dispatcher,
            NullLogger<QueuedJobWorker>.Instance,
            simulatedWorkDuration: TimeSpan.Zero);
        var pool = new JobWorkerPoolHostedService(
            worker,
            options,
            NullLogger<JobWorkerPoolHostedService>.Instance);

        for (var index = 0; index < 6; index++)
        {
            store.Add(CreateJob(index));
        }

        await pool.StartAsync(CancellationToken.None);

        await dispatcher.AllExecutionsCompleted.WaitAsync(TimeSpan.FromSeconds(5));
        await pool.StopAsync(CancellationToken.None);

        Assert.Equal(6, dispatcher.ExecutionCount);
        Assert.True(dispatcher.MaxConcurrentExecutions > 1);
        Assert.All(store.List(), job => Assert.Equal(JobStatus.Completed, job.Status));
    }

    private static JobRecord CreateJob(int index)
    {
        return JobRecord.Enqueue(
            Guid.NewGuid(),
            "send-welcome-email",
            Payload(),
            maxAttempts: 1,
            new DateTimeOffset(2026, 5, 4, 10, 0, index, TimeSpan.Zero));
    }

    private static IJobLifecycleService LifecycleService(IJobStore store)
    {
        return new JobLifecycleService(
            store,
            new JobDefinitionRegistry([new SendWelcomeEmailJobDefinition()]));
    }

    private static JsonElement Payload()
    {
        using var document = JsonDocument.Parse("""{"userId":"user_123","email":"person@example.com"}""");
        return document.RootElement.Clone();
    }

    private sealed class TrackingJobDispatcher : IJobDispatcher
    {
        private readonly int _expectedExecutions;
        private readonly TaskCompletionSource _allExecutionsCompleted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeExecutions;
        private int _executionCount;
        private int _maxConcurrentExecutions;

        public TrackingJobDispatcher(int expectedExecutions)
        {
            _expectedExecutions = expectedExecutions;
        }

        public Task AllExecutionsCompleted => _allExecutionsCompleted.Task;

        public int ExecutionCount => _executionCount;

        public int MaxConcurrentExecutions => _maxConcurrentExecutions;

        public async Task<JobExecutionResult> ExecuteAsync(
            JobRecord job,
            CancellationToken cancellationToken)
        {
            var activeExecutions = Interlocked.Increment(ref _activeExecutions);
            TrackMaxConcurrentExecutions(activeExecutions);

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

                if (Interlocked.Increment(ref _executionCount) == _expectedExecutions)
                {
                    _allExecutionsCompleted.SetResult();
                }

                return JobExecutionResult.Success();
            }
            finally
            {
                Interlocked.Decrement(ref _activeExecutions);
            }
        }

        private void TrackMaxConcurrentExecutions(int activeExecutions)
        {
            while (true)
            {
                var currentMax = _maxConcurrentExecutions;
                if (activeExecutions <= currentMax)
                {
                    return;
                }

                if (Interlocked.CompareExchange(
                    ref _maxConcurrentExecutions,
                    activeExecutions,
                    currentMax) == currentMax)
                {
                    return;
                }
            }
        }
    }
}
