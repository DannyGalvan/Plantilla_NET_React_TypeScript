using Project.SharedKernel.Domain;

namespace Project.Modules.Organization.Domain;

/// <summary>
/// Organization unit. \`Path\` stores the materialized hierarchy (e.g.
/// \"/a/b/c/\") so descendants can be queried with \`LIKE 'path%'\` indexed
/// portably across engines.
/// </summary>
public sealed class OrganizationUnit : AggregateRoot<Guid>
{
    public Guid TenantId { get; set; }
    public required string TypeCode { get; set; }
    public Guid? ParentId { get; set; }
    public required string Path { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class OrganizationUnitType : AggregateRoot<Guid>
{
    public Guid TenantId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? AllowedChildTypes { get; set; }
}

public sealed class OrganizationMembership : Entity<long>
{
    public Guid OrganizationUnitId { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
}