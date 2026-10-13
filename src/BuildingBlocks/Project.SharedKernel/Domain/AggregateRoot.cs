namespace Project.SharedKernel.Domain;

/// <summary>
/// Marker for aggregate roots, used by infrastructure to collect domain events.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

/// <summary>
/// Aggregate root: the only entry point that enforces invariants and raises domain events.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

/// <summary>
/// Convenience base for business aggregates: Guid v7 id, audit, soft delete and optimistic concurrency.
/// </summary>
public abstract class AuditableAggregateRoot : AggregateRoot<Guid>, IAuditable, ISoftDeletable, IHasConcurrencyStamp
{
    protected AuditableAggregateRoot()
    {
        Id = NewId.Next();
    }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
