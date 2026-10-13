namespace Project.Application.Tenancy;

/// <summary>Where a module stores its data.</summary>
public enum DataScope
{
    /// <summary>Always in the platform database (tenants catalog, users, settings).</summary>
    Platform,

    /// <summary>In the tenant database, which depends on <see cref="TenancyMode"/>.</summary>
    Tenant,
}

/// <summary>Deployment topology, selected with <c>Tenancy:Mode</c>.</summary>
public enum TenancyMode
{
    /// <summary>Single tenant. An implicit "default" tenant is used without any resolution or I/O.</summary>
    None,

    /// <summary>Many tenants in one database, isolated by a global <c>TenantId</c> filter.</summary>
    SharedDatabase,

    /// <summary>One database per tenant; the platform database holds the catalog.</summary>
    DatabasePerTenant,

    /// <summary>Each tenant is <see cref="TenantIsolation.Shared"/> or <see cref="TenantIsolation.Dedicated"/>.</summary>
    Hybrid,
}

public enum TenantIsolation
{
    Shared,
    Dedicated,
}

public enum TenantStatus
{
    Active,
    Suspended,
    Provisioning,
    Archived,
}

/// <summary>Immutable snapshot of a tenant, cached by the tenant store.</summary>
public sealed record TenantInfo(
    Guid Id,
    string Code,
    string Name,
    TenantStatus Status,
    TenantIsolation Isolation,
    string? DatabaseProvider,
    string? ConnectionSecretRef,
    string? Plan,
    IReadOnlyList<string> Features);

public static class TenantDefaults
{
    /// <summary>Id of the implicit tenant used in <see cref="TenancyMode.None"/> and seeded in every database.</summary>
    public static readonly Guid DefaultTenantId = new("00000000-0000-0000-0000-000000000001");

    public const string DefaultTenantCode = "default";
}

/// <summary>Current tenant of the request or background job. Scoped and immutable once resolved.</summary>
public interface ITenantContext
{
    TenancyMode Mode { get; }

    bool IsResolved { get; }

    TenantInfo? Tenant { get; }

    Guid? TenantId => Tenant?.Id;

    /// <summary>True when rows must be filtered by <c>TenantId</c> (shared database for the current tenant).</summary>
    bool UsesSharedDatabase { get; }
}

/// <summary>Sets the tenant. Only middleware, jobs, the outbox processor and the migration service use it.</summary>
public interface ITenantSetter
{
    void Set(TenantInfo tenant);
}

/// <summary>Cached tenant catalog implemented by the Tenancy module.</summary>
public interface ITenantStore
{
    ValueTask<TenantInfo?> FindByIdAsync(Guid id, CancellationToken ct);

    ValueTask<TenantInfo?> FindByCodeAsync(string code, CancellationToken ct);

    ValueTask<TenantInfo?> FindByHostAsync(string host, CancellationToken ct);

    Task<IReadOnlyList<TenantInfo>> ListActiveAsync(CancellationToken ct);
}

/// <summary>Resolves secret references (connection strings of dedicated tenants, API keys).</summary>
public interface ISecretStore
{
    ValueTask<string?> GetAsync(string reference, CancellationToken ct);
}

/// <summary>Runs work once per active tenant with a dedicated DI scope and tenant context.</summary>
public interface ITenantJobRunner
{
    Task ForEachTenantAsync(Func<IServiceProvider, TenantInfo, CancellationToken, Task> work, CancellationToken ct);

    /// <summary>
    /// Runs <paramref name="work"/> in a new scope bound to one tenant (OAuth callbacks, scheduled jobs, deliveries).
    /// Returns false when the tenant does not exist or is not active.
    /// </summary>
    Task<bool> RunForTenantAsync(Guid tenantId, Func<IServiceProvider, TenantInfo, CancellationToken, Task> work, CancellationToken ct);
}
