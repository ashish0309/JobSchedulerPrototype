using System.Collections.Concurrent;

namespace JobSchedulerPrototype.Jobs;

public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<Guid, JobRecord> _jobs = new();

    public void Add(JobRecord job)
    {
        _jobs[job.Id] = job;
    }

    public JobRecord? Get(Guid id)
    {
        return _jobs.GetValueOrDefault(id);
    }

    public IReadOnlyCollection<JobRecord> List()
    {
        return _jobs.Values
            .OrderBy(job => job.EnqueuedAt)
            .ToArray();
    }

    public JobRecord? TryClaimNextQueuedJob()
    {
        var queuedJobs = _jobs.Values
            .Where(job => job.Status == JobStatus.Queued)
            .OrderBy(job => job.EnqueuedAt);

        foreach (var job in queuedJobs)
        {
            var runningJob = job with { Status = JobStatus.Running };
            if (_jobs.TryUpdate(job.Id, runningJob, job))
            {
                return runningJob;
            }
        }

        return null;
    }

    public bool MarkCompleted(Guid id)
    {
        while (_jobs.TryGetValue(id, out var job))
        {
            if (job.Status != JobStatus.Running)
            {
                return false;
            }
            var completedJob = job with { Status = JobStatus.Completed };
            if (_jobs.TryUpdate(id, completedJob, job))
            {
                return true;
            }
        }

        return false;
    }
}
