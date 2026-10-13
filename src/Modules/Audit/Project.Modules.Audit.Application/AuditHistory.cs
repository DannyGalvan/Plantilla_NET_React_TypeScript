using System.Text.Json;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Auditing;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Modules.Audit.Contracts;
using Project.Modules.Audit.Domain;
using Project.SharedKernel;

namespace Project.Modules.Audit.Application;

/// <summary>
/// Stores the changes written to the outboxes of the other modules (tenant set by the outbox). Idempotent: the entry
/// id comes from the outbox message, so a retried message never records the change twice.
/// </summary>
public sealed class EntityChangedHandler(IRepository<AuditEntry, Guid> entries, IEventContext context) : IDomainEventHandler<EntityChangedEvent>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task HandleAsync(EntityChangedEvent domainEvent, CancellationToken ct)
    {
        var id = context.IdFor("audit");
        if (id is { } existing && await entries.AnyAsync(new Specification<AuditEntry>().Where(e => e.Id == existing), ct))
        {
            return;
        }

        entries.Add(AuditEntry.Create(
            id,
            domainEvent.EntityType,
            domainEvent.EntityId,
            domainEvent.Operation,
            JsonSerializer.Serialize(domainEvent.Changes, Json),
            domainEvent.UserId,
            domainEvent.UserName,
            domainEvent.OccurredAt));
        await entries.UnitOfWork.SaveChangesAsync(ct);
    }
}

public sealed record AuditEntryDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Operation,
    IReadOnlyList<PropertyChange> Changes,
    string? UserId,
    string? UserName,
    DateTimeOffset OccurredAt);

public sealed record ListAuditEntriesQuery(
    string? EntityType,
    string? EntityId,
    string? UserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedResult<AuditEntryDto>>, IRequirePermission
{
    public string Permission => AuditPermissions.History.View;
}

public sealed class ListAuditEntriesHandler(IRepository<AuditEntry, Guid> entries) : IQueryHandler<ListAuditEntriesQuery, PagedResult<AuditEntryDto>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async ValueTask<Result<PagedResult<AuditEntryDto>>> Handle(ListAuditEntriesQuery query, CancellationToken cancellationToken)
    {
        var spec = new Specification<AuditEntry>();
        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            spec.Where(e => e.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            spec.Where(e => e.EntityId == query.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            spec.Where(e => e.UserId == query.UserId);
        }

        if (query.From is { } from)
        {
            spec.Where(e => e.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            spec.Where(e => e.OccurredAt <= to);
        }

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 200);
        var total = await entries.CountAsync(spec, cancellationToken);
        var items = await entries.ListAsync(spec.OrderBy(e => e.OccurredAt, descending: true).Page((page - 1) * size, size), cancellationToken);
        return new PagedResult<AuditEntryDto>(
            items.Select(e => new AuditEntryDto(
                e.Id,
                e.EntityType,
                e.EntityId,
                e.Operation,
                JsonSerializer.Deserialize<List<PropertyChange>>(e.Changes, Json) ?? [],
                e.UserId,
                e.UserName,
                e.OccurredAt)).ToList(),
            page,
            size,
            total);
    }
}

public sealed class AuditPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(AuditPermissions.History.View, AuditPermissions.Module, "permissions.audit.history.view", "history"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("audit.history", AuditPermissions.Module, "nav.audit", "/audit", "history", AuditPermissions.History.View, 800),
    ];
}
