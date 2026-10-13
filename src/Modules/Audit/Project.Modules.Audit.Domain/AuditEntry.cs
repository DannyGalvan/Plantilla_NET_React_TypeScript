using Project.SharedKernel.Domain;

namespace Project.Modules.Audit.Domain;

/// <summary>One change of a business record: who, when, what operation and which values changed (JSON).</summary>
public sealed class AuditEntry : AggregateRoot<Guid>, IMultiTenant
{
    private AuditEntry()
    {
    }

    public Guid TenantId { get; set; }

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string Operation { get; private set; } = string.Empty;

    /// <summary>JSON array of <c>{ property, oldValue, newValue }</c>.</summary>
    public string Changes { get; private set; } = "[]";

    public string? UserId { get; private set; }

    public string? UserName { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static AuditEntry Create(string entityType, string entityId, string operation, string changes, string? userId, string? userName, DateTimeOffset occurredAt) =>
        new()
        {
            Id = NewId.Next(),
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation,
            Changes = changes,
            UserId = userId,
            UserName = userName,
            OccurredAt = occurredAt,
        };
}
