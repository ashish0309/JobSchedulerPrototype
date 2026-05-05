namespace JobSchedulerPrototype.Jobs;

public interface IJobWorker
{
    Task<bool> ProcessNextJobAsync(CancellationToken cancellationToken);
}
