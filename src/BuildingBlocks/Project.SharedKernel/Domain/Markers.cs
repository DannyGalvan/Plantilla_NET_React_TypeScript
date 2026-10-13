namespace Project.SharedKernel.Domain;

/// <summary>
/// Marker for domain events raised by aggregate roots. Events are persisted to the outbox
/// in the same transaction and dispatched asynchronously.
/// </summary>
public interface IDomainEvent;

/// <summary>Records who created or modified the entity and when (filled by an EF interceptor).</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }

    string? CreatedBy { get; set; }

    DateTimeOffset? UpdatedAt { get; set; }

    string? UpdatedBy { get; set; }
}

/// <summary>Logical delete: rows are flagged instead of removed (filled by an EF interceptor).</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedAt { get; set; }

    string? DeletedBy { get; set; }
}

/// <summary>Portable optimistic concurrency token, regenerated on every write.</summary>
public interface IHasConcurrencyStamp
{
    Guid ConcurrencyStamp { get; set; }
}

/// <summary>
/// Data that belongs to a tenant. The column always exists, so a project can move from
/// <c>Tenancy:Mode=None</c> to a shared database without changing the model.
/// </summary>
public interface IMultiTenant
{
    Guid TenantId { get; set; }
}

/// <summary>Data that belongs to an organization unit (company, country, brand, branch...).</summary>
public interface IOrganizationScoped
{
    Guid OrganizationUnitId { get; set; }
}

/// <summary>Data owned by a single user, exposed through <c>/me</c> endpoints.</summary>
public interface IOwned
{
    string OwnerId { get; set; }
}
