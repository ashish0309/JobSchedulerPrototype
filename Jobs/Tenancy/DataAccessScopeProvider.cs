namespace JobSchedulerPrototype.Jobs;

public sealed class DataAccessScopeProvider : IDataAccessScopeProvider
{
    private readonly IJobActorProvider _actorProvider;
    private readonly AsyncLocal<JobActor?> _currentActor = new();
    private readonly AsyncLocal<DataAccessScope?> _currentScope = new();
    private readonly AsyncLocal<DataAccessOperation?> _currentOperation = new();
    private readonly AsyncLocal<int> _crossTenantScopeDepth = new();

    public DataAccessScopeProvider(IJobActorProvider actorProvider)
    {
        _actorProvider = actorProvider;
    }

    public JobActor? ScopedActor => _currentActor.Value;

    public JobActor CurrentActor => ScopedActor ?? _actorProvider.GetCurrentActor();

    public DataAccessScope Current =>
        _currentScope.Value ?? DataAccessScope.Tenant(CurrentActor.TenantId);

    public DataAccessOperation CurrentOperation =>
        _currentOperation.Value ?? DataAccessOperation.Read;

    public IDisposable BeginScope(DataAccessScope scope, DataAccessOperation operation)
    {
        if (scope.IncludesAllTenants && _crossTenantScopeDepth.Value == 0)
        {
            throw new InvalidOperationException(
                "Cross-tenant scope requires BeginCrossTenantScope.");
        }

        var previousScope = _currentScope.Value;
        var previousOperation = _currentOperation.Value;
        _currentScope.Value = scope;
        _currentOperation.Value = operation;

        return new ScopeHandle(() =>
        {
            _currentScope.Value = previousScope;
            _currentOperation.Value = previousOperation;
        });
    }

    public IDisposable BeginScope(DataAccessScope scope)
    {
        return BeginScope(scope, DataAccessOperation.Read);
    }

    public IDisposable BeginCrossTenantScope(DataAccessOperation operation, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var actor = CurrentActor;
        if (!CanUseCrossTenantScope(actor, operation))
        {
            throw new InvalidOperationException(
                $"Actor '{actor.Id}' is not authorized for cross-tenant '{operation}' scope.");
        }

        var previousDepth = _crossTenantScopeDepth.Value;
        _crossTenantScopeDepth.Value = previousDepth + 1;
        var scopeHandle = BeginScope(DataAccessScope.AllTenants(), operation);

        return new ScopeHandle(() =>
        {
            scopeHandle.Dispose();
            _crossTenantScopeDepth.Value = previousDepth;
        });
    }

    public IDisposable BeginActorScope(JobActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var previousActor = _currentActor.Value;
        _currentActor.Value = actor;

        return new ScopeHandle(() => _currentActor.Value = previousActor);
    }

    private static bool CanUseCrossTenantScope(
        JobActor actor,
        DataAccessOperation operation)
    {
        if (actor.HasPermission(JobPermissions.All))
        {
            return true;
        }

        return operation switch
        {
            DataAccessOperation.Read => actor.HasPermission(JobPermissions.GlobalRead)
                                        || actor.HasPermission(JobPermissions.Execute),
            DataAccessOperation.Mutate => actor.HasPermission(JobPermissions.GlobalRead)
                                          || actor.HasPermission(JobPermissions.Execute),
            _ => false
        };
    }

    private sealed class ScopeHandle : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public ScopeHandle(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _dispose();
        }
    }
}
