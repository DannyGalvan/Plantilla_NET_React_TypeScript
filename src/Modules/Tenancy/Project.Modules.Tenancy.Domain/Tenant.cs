using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Modules.Tenancy.Domain;

public enum TenantState
{
    Active,
    Suspended,
    Provisioning,
    Archived,
}

public enum DatabaseIsolation
{
    Shared,
    Dedicated,
}

/// <summary>
/// Tenant aggregate: the isolation boundary (data, security, configuration and optionally the database).
/// The connection string of a dedicated tenant is never stored: only a reference to the secret.
/// </summary>
public sealed class Tenant : AuditableAggregateRoot
{
    private readonly List<TenantDomain> _domains = [];
    private readonly List<TenantFeature> _features = [];

    private Tenant()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public TenantState Status { get; private set; }

    public DatabaseIsolation Isolation { get; private set; }

    public string? DatabaseProvider { get; private set; }

    public string? ConnectionSecretRef { get; private set; }

    public string? Plan { get; private set; }

    public IReadOnlyCollection<TenantDomain> Domains => _domains;

    public IReadOnlyCollection<TenantFeature> Features => _features;

    public static Tenant Create(
        string code,
        string name,
        DatabaseIsolation isolation,
        string? databaseProvider,
        string? connectionSecretRef,
        string? plan,
        Guid? id = null)
    {
        var tenant = new Tenant
        {
            Id = id ?? NewId.Next(),
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Isolation = isolation,
            DatabaseProvider = databaseProvider,
            ConnectionSecretRef = connectionSecretRef,
            Plan = plan,
            Status = isolation == DatabaseIsolation.Dedicated ? TenantState.Provisioning : TenantState.Active,
        };
        tenant.Raise(new TenantChanged(tenant.Id));
        return tenant;
    }

    public void Update(string name, string? plan)
    {
        Name = name.Trim();
        Plan = plan;
        Raise(new TenantChanged(Id));
    }

    public Result ChangeStatus(TenantState status)
    {
        if (Status == TenantState.Archived && status != TenantState.Archived)
        {
            return Error.Conflict("tenant.archived", "An archived tenant cannot be reactivated.");
        }

        Status = status;
        Raise(new TenantChanged(Id));
        return Result.Success();
    }

    public Result<TenantDomain> AddDomain(string host, bool isPrimary)
    {
        var normalized = host.Trim().ToLowerInvariant();
        if (_domains.Any(d => d.Host == normalized))
        {
            return Error.Conflict("tenant.domain_exists", "The domain is already registered for this tenant.");
        }

        if (isPrimary)
        {
            _domains.ForEach(d => d.IsPrimary = false);
        }

        var domain = new TenantDomain(NewId.Next(), Id, normalized, isPrimary || _domains.Count == 0);
        _domains.Add(domain);
        Raise(new TenantChanged(Id));
        return domain;
    }

    public Result RemoveDomain(Guid domainId)
    {
        var domain = _domains.FirstOrDefault(d => d.Id == domainId);
        if (domain is null)
        {
            return Error.NotFound("tenant.domain_not_found");
        }

        _domains.Remove(domain);
        Raise(new TenantChanged(Id));
        return Result.Success();
    }

    public void SetFeature(string feature, bool enabled)
    {
        var key = feature.Trim().ToLowerInvariant();
        var existing = _features.FirstOrDefault(f => f.Feature == key);
        if (existing is null)
        {
            _features.Add(new TenantFeature(NewId.Next(), Id, key, enabled));
        }
        else
        {
            existing.Enabled = enabled;
        }

        Raise(new TenantChanged(Id));
    }
}

/// <summary>Host name that resolves to the tenant (custom domain or subdomain).</summary>
public sealed class TenantDomain : Entity<Guid>
{
    private TenantDomain()
    {
    }

    internal TenantDomain(Guid id, Guid tenantId, string host, bool isPrimary)
    {
        Id = id;
        TenantId = tenantId;
        Host = host;
        IsPrimary = isPrimary;
    }

    public Guid TenantId { get; private set; }

    public string Host { get; private set; } = string.Empty;

    public bool IsPrimary { get; set; }
}

/// <summary>Feature switched on or off for one tenant, overriding the configuration default.</summary>
public sealed class TenantFeature : Entity<Guid>
{
    private TenantFeature()
    {
    }

    internal TenantFeature(Guid id, Guid tenantId, string feature, bool enabled)
    {
        Id = id;
        TenantId = tenantId;
        Feature = feature;
        Enabled = enabled;
    }

    public Guid TenantId { get; private set; }

    public string Feature { get; private set; } = string.Empty;

    public bool Enabled { get; set; }
}

/// <summary>
/// Settings override stored as JSON for one section at one scope (System, Tenant, OrganizationUnit, User).
/// </summary>
public sealed class SettingValue : AggregateRoot<Guid>
{
    private SettingValue()
    {
    }

    /// <summary>System, Tenant, OrganizationUnit or User.</summary>
    public string Scope { get; private set; } = string.Empty;

    /// <summary>Tenant, unit or user id; null for System.</summary>
    public Guid? ScopeId { get; private set; }

    /// <summary>Owning tenant (null for System), used for isolation of unit and user overrides.</summary>
    public Guid? TenantId { get; private set; }

    public string SectionKey { get; private set; } = string.Empty;

    public string Json { get; private set; } = "{}";

    public DateTimeOffset UpdatedAt { get; private set; }

    public static SettingValue Create(string scope, Guid? scopeId, Guid? tenantId, string sectionKey, string json, DateTimeOffset now)
    {
        var value = new SettingValue
        {
            Id = NewId.Next(),
            Scope = scope,
            ScopeId = scopeId,
            TenantId = tenantId,
            SectionKey = sectionKey,
            Json = json,
            UpdatedAt = now,
        };
        value.Raise(new SettingsChanged(sectionKey));
        return value;
    }

    public void Update(string json, DateTimeOffset now)
    {
        Json = json;
        UpdatedAt = now;
        Raise(new SettingsChanged(SectionKey));
    }
}

/// <summary>Raised when a tenant, its domains or its features change. Invalidates caches.</summary>
public sealed record TenantChanged(Guid TenantId) : IDomainEvent;

/// <summary>Raised when a settings value changes at any scope.</summary>
public sealed record SettingsChanged(string SectionKey) : IDomainEvent;
