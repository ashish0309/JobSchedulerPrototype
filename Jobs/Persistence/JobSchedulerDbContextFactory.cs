using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobSchedulerPrototype.Jobs;

public sealed class JobSchedulerDbContextFactory : IDesignTimeDbContextFactory<JobSchedulerDbContext>
{
    private const string DefaultConnectionString = "Data Source=jobscheduler.db";
    private static readonly JobActor DesignTimeActor = new(
        "design-time",
        "system",
        [JobPermissions.All]);

    public JobSchedulerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JobSchedulerDbContext>();
        optionsBuilder.UseSqlite(DefaultConnectionString);

        var scopeProvider = new DataAccessScopeProvider(
            new StaticJobActorProvider(DesignTimeActor));

        return new JobSchedulerDbContext(optionsBuilder.Options, scopeProvider);
    }

    private sealed class StaticJobActorProvider : IJobActorProvider
    {
        private readonly JobActor _actor;

        public StaticJobActorProvider(JobActor actor)
        {
            _actor = actor;
        }

        public JobActor GetCurrentActor()
        {
            return _actor;
        }
    }
}
