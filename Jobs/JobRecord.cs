using System.Text.Json;

namespace JobSchedulerPrototype.Jobs;

public sealed record JobRecord(
    Guid Id,
    string Type,
    JsonElement Payload,
    JobStatus Status,
    DateTimeOffset EnqueuedAt);
