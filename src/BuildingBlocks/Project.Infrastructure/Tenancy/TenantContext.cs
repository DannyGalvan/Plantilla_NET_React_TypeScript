using Microsoft.Extensions.Options;
using Project.Application.Tenancy;

namespace Project.Infrastructure.Tenancy;

/// <summary>
/// Scoped tenant context. In <see cref="TenancyMode.None"/> it always exposes the implicit default tenant
/// without any I/O; otherwise the resolution middleware, jobs or the outbox set it once.
/// </summary>
public sealed class TenantContext(IOptions<TenancyOptions> options) : ITenantContext, ITenantSetter
{
    public static readonly TenantInfo DefaultTenant = new(
        TenantDefaults.DefaultTenantId,
        TenantDefaults.DefaultTenantCode,
        "Default",
        TenantStatus.Active,
        TenantIsolation.Shared,
        DatabaseProvider: null,
        ConnectionSecretRef: null,
        Plan: null,
        Features: []);

    private TenantInfo? _tenant;

    public TenancyMode Mode { get; } = options.Value.Mode;

    public bool IsResolved => Tenant is not null;

    public TenantInfo? Tenant => _tenant ?? (Mode == TenancyMode.None ? DefaultTenant : null);

    public Guid? TenantId => Tenant?.Id;

    public bool UsesSharedDatabase => Mode switch
    {
        TenancyMode.None => false,
        TenancyMode.SharedDatabase => true,
        _ => !UsesDedicatedDatabase,
    };

    /// <summary>
    /// True when the current tenant has its own database. In <see cref="TenancyMode.DatabasePerTenant"/> new tenants
    /// are always dedicated; the implicit default tenant (if used) lives in the platform database.
    /// </summary>
    public bool UsesDedicatedDatabase =>
        Mode is TenancyMode.DatabasePerTenant or TenancyMode.Hybrid && Tenant?.Isolation == TenantIsolation.Dedicated;

    public void Set(TenantInfo tenant)
    {
        if (_tenant is not null && _tenant.Id != tenant.Id)
        {
            throw new InvalidOperationException("The tenant of a scope cannot change once it has been resolved.");
        }

        _tenant = tenant;
    }
}
