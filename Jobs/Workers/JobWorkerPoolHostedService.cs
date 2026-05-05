using Microsoft.Extensions.Options;

namespace JobSchedulerPrototype.Jobs;

public sealed class JobWorkerPoolHostedService : BackgroundService
{
    private readonly QueuedJobWorker _worker;
    private readonly ILogger<JobWorkerPoolHostedService> _logger;
    private readonly JobWorkerOptions _options;

    public JobWorkerPoolHostedService(
        QueuedJobWorker worker,
        IOptions<JobWorkerOptions> options,
        ILogger<JobWorkerPoolHostedService> logger)
    {
        _worker = worker;
        _logger = logger;
        _options = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerCount = _options.ValidWorkerCount;

        _logger.LogInformation(
            "Starting job worker pool with {WorkerCount} worker(s)",
            workerCount);

        var workers = Enumerable
            .Range(1, workerCount)
            .Select(workerNumber => RunWorkerLoopAsync(workerNumber, stoppingToken));

        return Task.WhenAll(workers);
    }

    private async Task RunWorkerLoopAsync(
        int workerNumber,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processedJob = await _worker.ProcessNextJobAsync(stoppingToken);
            if (!processedJob)
            {
                _logger.LogDebug(
                    "Job worker {WorkerNumber} found no due job",
                    workerNumber);

                await Task.Delay(_options.ValidPollInterval, stoppingToken);
            }
        }
    }
}
