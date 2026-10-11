using Project.SharedKernel.Domain;

namespace Project.Modules.Tenancy.Domain;

/// <summary>
/// Tenant aggregate. \`Status\` transitions Active → Suspended → Provisioning →
/// Archived. \`ConnectionSecretRef\` stores the secret reference (Key Vault, AWS
/// Secrets Manager, env var) — the actual connection string is never written
/// here. \`Isolation\` is Shared or Dedicated; Dedicated tenants have their
/// own database connection (see ITenantConnectionResolver).
/// </summary>
public sealed class Tenant : AggregateRoot<Guid>
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string Status { get; set; } = "Active";
    public string Isolation { get; set; } = "Shared";
    public string? DatabaseProvider { get; set; }
    public string? ConnectionSecretRef { get; set; }
    public string? Plan { get; set; }
}

public sealed class TenantDomain : Entity<long>
{
    public Guid TenantId { get; set; }
    public required string Host { get; set; }
    public bool IsPrimary { get; set; }
    public bool Verified { get; set; }
}

public sealed class TenantFeature : Entity<long>
{
    public Guid TenantId { get; set; }
    public required string Feature { get; set; }
    public bool Enabled { get; set; }
}

public sealed class SettingValue : Entity<long>
{
    /// <summary>System, Tenant, OrganizationUnit or User.</summary>
    public required string Scope { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public Guid? UserId { get; set; }
    public required string SectionKey { get; set; }
    public required string JsonValue { get; set; }
    public long Version { get; set; }
}