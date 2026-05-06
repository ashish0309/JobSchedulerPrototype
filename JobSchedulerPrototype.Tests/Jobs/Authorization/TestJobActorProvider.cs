using JobSchedulerPrototype.Jobs;

namespace JobSchedulerPrototype.Tests.Jobs;

internal sealed class TestJobActorProvider : IJobActorProvider
{
    public const string ActorId = "user-123";
    public const string TenantId = "tenant-alpha";

    private readonly JobActor _actor;

    public TestJobActorProvider()
        : this(new JobActor(ActorId, TenantId, [JobPermissions.All]))
    {
    }

    public TestJobActorProvider(JobActor actor)
    {
        _actor = actor;
    }

    public JobActor GetCurrentActor()
    {
        return _actor;
    }
}
