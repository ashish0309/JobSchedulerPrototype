using System.Text.Json;

namespace JobSchedulerPrototype.Jobs;

public sealed record JobRecord
{
    public Guid Id { get; private init; }
    public string Type { get; private init; }
    public JsonElement Payload { get; private init; }
    public JobStatus Status { get; private init; }
    public IReadOnlyList<JobStateChange> History { get; private init; }

    public DateTimeOffset EnqueuedAt => History[0].ChangedAt;

    private JobRecord(
        Guid id,
        string type,
        JsonElement payload,
        JobStatus status,
        IReadOnlyList<JobStateChange> history)
    {
        Id = id;
        Type = type;
        Payload = payload;
        Status = status;
        History = history;
    }

    public static JobRecord Enqueue(
        Guid id,
        string type,
        JsonElement payload,
        DateTimeOffset enqueuedAt)
    {
        return new JobRecord(
            id,
            type,
            payload,
            JobStatus.Queued,
            [new JobStateChange(JobStatus.Queued, enqueuedAt)]);
    }

    public JobRecord TransitionTo(JobStatus nextStatus, DateTimeOffset changedAt)
    {
        return this with
        {
            Status = nextStatus,
            History = [.. History, new JobStateChange(nextStatus, changedAt)]
        };
    }

    public DateTimeOffset QueuedAt()
    {
        return EnqueuedAt;
    }
}

public sealed record JobStateChange(
    JobStatus Status,
    DateTimeOffset ChangedAt);
