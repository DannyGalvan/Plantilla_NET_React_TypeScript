namespace Project.SharedKernel.Domain;

/// <summary>
/// Marks an entity that records who created or modified it and when.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}

/// <summary>
/// Marks an entity that is soft-deleted (preserved in storage).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}

/// <summary>
/// Optimistic concurrency via a portable token (Guid instead of rowversion).
/// </summary>
public interface IHasConcurrencyStamp
{
    Guid ConcurrencyStamp { get; set; }
}

/// <summary>
/// Tenant isolation marker. SharedDatabase and Hybrid-shared apply a global filter
/// on <see cref="TenantId"/>; in DatabasePerTenant the column is filled with the
/// default tenant for portability across topologies.
/// </summary>
public interface IMultiTenant
{
    Guid TenantId { get; set; }
}

/// <summary>
/// Organizational scope marker (Company, Brand, Country, Branch).
/// </summary>
public interface IOrganizationScoped
{
    Guid OrganizationUnitId { get; set; }
}

/// <summary>
/// Marks an entity that is owned by a user (typically <c>OwnerId == currentUser</c>).
/// </summary>
public interface IOwned
{
    string OwnerId { get; }
}