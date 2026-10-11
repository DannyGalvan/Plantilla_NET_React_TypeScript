namespace Project.Infrastructure.Outbox;

/// <summary>
/// Outbox row persisted by <see cref="Persistence.Interceptors.OutboxInterceptor"/>
/// and dispatched by <see cref="OutboxProcessor"/>. Each module owns its own
/// <c>OutboxMessages</c> table in its schema.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Error { get; set; }
}