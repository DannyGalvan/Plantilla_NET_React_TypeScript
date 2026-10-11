namespace Project.Infrastructure.Tenancy;

/// <summary>
/// Default scoped implementation. When <see cref="Mode"/> is <see cref="TenancyMode.None"/>
/// it returns a synthetic "default" tenant without doing any I/O.
/// </summary>
public sealed class TenantContext : ITenantContext, ITenantSetter
{
    private readonly TenancyMode _mode;

    public TenantContext(TenancyMode mode)
    {
        _mode = mode;
        Tenant = _mode == TenancyMode.None
            ? new TenantInfo(Guid.Empty, "default", "Default", TenantIsolation.Shared, null, null, "Active", new HashSet<string>())
            : null;
    }

    public TenancyMode Mode => _mode;
    public bool IsResolved => Tenant is not null;
    public TenantInfo? Tenant { get; private set; }

    public void Set(TenantInfo tenant) => Tenant = tenant;
    public void Clear() => Tenant = _mode == TenancyMode.None ? Tenant : null;
}