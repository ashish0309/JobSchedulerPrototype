namespace JobSchedulerPrototype.Jobs;

public interface IJobWorker
{
    Task<bool> ProcessNextJobAsync(string workerId, CancellationToken cancellationToken);
}
