namespace Project.Infrastructure.Tenancy;

public enum TenancyMode
{
    None,
    SharedDatabase,
    DatabasePerTenant,
    Hybrid,
}

public enum TenantIsolation
{
    Shared,
    Dedicated,
}

/// <summary>
/// Scoped, immutable once resolved. The <see cref="Tenant"/> is set by the
/// resolution middleware (or by background workers and the outbox processor).
/// </summary>
public interface ITenantContext
{
    TenancyMode Mode { get; }
    bool IsResolved { get; }
    TenantInfo? Tenant { get; }
    Guid? TenantId => Tenant?.Id;
}

public interface ITenantSetter
{
    void Set(TenantInfo tenant);
    void Clear();
}

public sealed record TenantInfo(
    Guid Id,
    string Code,
    string Name,
    TenantIsolation Isolation,
    string? DatabaseProvider,
    string? ConnectionSecretRef,
    string Status,
    IReadOnlySet<string> Features);

/// <summary>
/// Returns the configured tenant code for the current request, or null if
/// the tenant cannot be resolved. Strategies are ordered in configuration.
/// </summary>
public interface ITenantResolver
{
    int Order { get; }
    ValueTask<string?> ResolveCodeAsync(HttpContext context, CancellationToken ct);
}

/// <summary>
/// Cached tenant catalog. The default implementation reads from
/// <c>Project.Infrastructure.Tenancy.TenantStore</c> and is keyed with
/// <c>tenant:{id}</c> via HybridCache.
/// </summary>
public interface ITenantStore
{
    ValueTask<TenantInfo?> FindByCodeAsync(string code, CancellationToken ct);
    ValueTask<TenantInfo?> FindByHostAsync(string host, CancellationToken ct);
    IAsyncEnumerable<TenantInfo> ListActiveAsync(CancellationToken ct);
    ValueTask InvalidateAsync(Guid tenantId, CancellationToken ct);
}

/// <summary>
/// Resolves the EF Core connection for a (module, tenant) pair. Connection
/// strings never come from configuration directly.
/// </summary>
public interface ITenantConnectionResolver
{
    (string Provider, string ConnectionString) Resolve(string module, TenantInfo? tenant);
}

public interface ISecretStore
{
    ValueTask<string> GetAsync(string reference, CancellationToken ct);
}

/// <summary>
/// Used by the global filters in <see cref="Persistence.ModuleDbContext"/>.
/// Returns NoOp when the Organization module is not registered.
/// </summary>
public interface IOrganizationContext
{
    bool Enabled { get; }
    Guid? ActiveUnitId { get; }
    IReadOnlySet<Guid> AccessibleUnitIds { get; }
}