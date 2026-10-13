using Project.SharedKernel.Domain;

namespace Project.Application.Auditing;

public sealed record PropertyChange(string Property, string? OldValue, string? NewValue);

/// <summary>
/// A business record was created, changed, deleted or restored. Written to the outbox of the module that owns the
/// record in the same transaction; the Audit module stores it in the history of the tenant.
/// </summary>
/// <param name="EntityType">Schema of the module and entity name, for example <c>catalog.product</c>.</param>
public sealed record EntityChangedEvent(
    string EntityType,
    string EntityId,
    string Operation,
    IReadOnlyList<PropertyChange> Changes,
    string? UserId,
    string? UserName,
    DateTimeOffset OccurredAt) : IDomainEvent;

public static class AuditOperations
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";
    public const string Restored = "restored";
}
