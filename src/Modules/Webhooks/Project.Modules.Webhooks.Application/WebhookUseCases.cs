using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Application.Webhooks;
using Project.Modules.Webhooks.Contracts;
using Project.Modules.Webhooks.Domain;
using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Modules.Webhooks.Application;

public static class WebhookPayloads
{
    public const string SecretPurpose = "webhooks.signing-secret";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The body sent to the endpoint: <c>{ id, event, occurredAt, data }</c>.</summary>
    public static string Create(Guid id, string eventName, DateTimeOffset occurredAt, object data) =>
        JsonSerializer.Serialize(new { id, @event = eventName, occurredAt, data }, Json);

    public static string NewSecret() => "whsec_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// Turns every published event into deliveries for the subscriptions of the tenant that want it. Idempotent: the
/// delivery id (also the <c>id</c> receivers use to deduplicate) comes from the outbox message and the subscription.
/// </summary>
public sealed class WebhookEventObserver(
    IEnumerable<WebhookEventDefinition> definitions,
    IRepository<WebhookSubscription, Guid> subscriptions,
    IRepository<WebhookDelivery, Guid> deliveries,
    IEventContext context,
    TimeProvider clock) : IDomainEventObserver
{
    public async Task OnEventAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var definition = definitions.FirstOrDefault(d => d.EventType == domainEvent.GetType());
        if (definition is null)
        {
            return;
        }

        var targets = (await subscriptions.ListAsync(new Specification<WebhookSubscription>().Where(s => s.IsActive), ct))
            .Where(s => s.Wants(definition.Name))
            .ToList();
        if (targets.Count == 0)
        {
            return;
        }

        var now = clock.GetUtcNow();
        var ids = targets.ToDictionary(s => s.Id, s => context.IdFor($"webhook:{definition.Name}:{s.Id:N}") ?? NewId.Next());
        var candidates = ids.Values.ToList();
        var existing = (await deliveries.ListAsync(new Specification<WebhookDelivery>().Where(d => candidates.Contains(d.Id)), ct)).Select(d => d.Id).ToHashSet();
        foreach (var subscription in targets.Where(s => !existing.Contains(ids[s.Id])))
        {
            var id = ids[subscription.Id];
            deliveries.Add(WebhookDelivery.Create(id, subscription.Id, definition.Name, WebhookPayloads.Create(id, definition.Name, now, domainEvent), now));
        }

        await deliveries.UnitOfWork.SaveChangesAsync(ct);
    }
}

public sealed record SubscriptionDto(Guid Id, string Name, string Url, IReadOnlyList<string> Events, bool IsActive, DateTimeOffset CreatedAt)
{
    public static SubscriptionDto From(WebhookSubscription s) =>
        new(s.Id, s.Name, s.Url, s.Events.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), s.IsActive, s.CreatedAt);
}

/// <summary>Returned once at creation: the secret to verify signatures is never shown again.</summary>
public sealed record CreatedSubscriptionDto(SubscriptionDto Subscription, string Secret);

public sealed record WebhookEventDto(string Name, string? Description);

public sealed record ListWebhookEventsQuery : IQuery<IReadOnlyList<WebhookEventDto>>, IRequirePermission
{
    public string Permission => WebhooksPermissions.Subscriptions.Manage;
}

public sealed class ListWebhookEventsHandler(IEnumerable<WebhookEventDefinition> definitions)
    : IQueryHandler<ListWebhookEventsQuery, IReadOnlyList<WebhookEventDto>>
{
    public ValueTask<Result<IReadOnlyList<WebhookEventDto>>> Handle(ListWebhookEventsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<WebhookEventDto> events = definitions.Select(d => new WebhookEventDto(d.Name, d.Description)).OrderBy(e => e.Name, StringComparer.Ordinal).ToList();
        return ValueTask.FromResult(Result<IReadOnlyList<WebhookEventDto>>.Success(events));
    }
}

public sealed record ListSubscriptionsQuery : IQuery<IReadOnlyList<SubscriptionDto>>, IRequirePermission
{
    public string Permission => WebhooksPermissions.Subscriptions.Manage;
}

public sealed class ListSubscriptionsHandler(IRepository<WebhookSubscription, Guid> subscriptions)
    : IQueryHandler<ListSubscriptionsQuery, IReadOnlyList<SubscriptionDto>>
{
    public async ValueTask<Result<IReadOnlyList<SubscriptionDto>>> Handle(ListSubscriptionsQuery query, CancellationToken cancellationToken) =>
        (await subscriptions.ListAsync(new Specification<WebhookSubscription>().OrderBy(s => s.Name), cancellationToken)).Select(SubscriptionDto.From).ToList();
}

/// <summary>Checks a webhook URL before saving it (scheme and network policy of the platform).</summary>
public interface IWebhookUrlPolicy
{
    string? Validate(string url);
}

public sealed record SaveSubscriptionCommand(Guid? Id, string Name, string Url, IReadOnlyList<string> Events, bool IsActive)
    : ICommand<CreatedSubscriptionDto>, IRequirePermission
{
    public string Permission => WebhooksPermissions.Subscriptions.Manage;
}

public sealed class SaveSubscriptionValidator : AbstractValidator<SaveSubscriptionCommand>
{
    public SaveSubscriptionValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Url).NotEmpty().MaximumLength(1024);
        RuleFor(c => c.Events).NotEmpty();
    }
}

/// <summary>Creates (new secret, returned once) or updates a subscription.</summary>
public sealed class SaveSubscriptionHandler(
    IRepository<WebhookSubscription, Guid> subscriptions,
    IEnumerable<WebhookEventDefinition> definitions,
    IWebhookUrlPolicy urls,
    ISecretProtector protector) : ICommandHandler<SaveSubscriptionCommand, CreatedSubscriptionDto>
{
    public async ValueTask<Result<CreatedSubscriptionDto>> Handle(SaveSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (urls.Validate(command.Url) is { } problem)
        {
            return Error.Validation("url", problem);
        }

        var known = definitions.Select(d => d.Name).Append(WebhookEvents.Ping).ToHashSet(StringComparer.Ordinal);
        var unknown = command.Events.Where(e => e != "*" && !known.Contains(e)).ToList();
        if (unknown.Count > 0)
        {
            return Error.Validation("events", $"Unknown events: {string.Join(", ", unknown)}.");
        }

        var events = command.Events.Contains("*") ? "*" : string.Join(',', command.Events.Distinct(StringComparer.Ordinal));
        if (command.Id is { } id)
        {
            var existing = await subscriptions.FirstOrDefaultAsync(new Specification<WebhookSubscription>().Where(s => s.Id == id).Tracked(), cancellationToken);
            if (existing is null)
            {
                return Error.NotFound("webhooks.subscription_not_found");
            }

            existing.Name = command.Name.Trim();
            existing.Url = command.Url.Trim();
            existing.Events = events;
            existing.IsActive = command.IsActive;
            await subscriptions.UnitOfWork.SaveChangesAsync(cancellationToken);
            return new CreatedSubscriptionDto(SubscriptionDto.From(existing), string.Empty);
        }

        var secret = WebhookPayloads.NewSecret();
        var subscription = new WebhookSubscription
        {
            Name = command.Name.Trim(),
            Url = command.Url.Trim(),
            Events = events,
            IsActive = command.IsActive,
            ProtectedSecret = protector.Protect(WebhookPayloads.SecretPurpose, secret),
        };
        subscriptions.Add(subscription);
        await subscriptions.UnitOfWork.SaveChangesAsync(cancellationToken);
        return new CreatedSubscriptionDto(SubscriptionDto.From(subscription), secret);
    }
}

public sealed record DeleteSubscriptionCommand(Guid Id) : ICommand, IRequirePermission
{
    public string Permission => WebhooksPermissions.Subscriptions.Manage;
}

public sealed class DeleteSubscriptionHandler(IRepository<WebhookSubscription, Guid> subscriptions) : ICommandHandler<DeleteSubscriptionCommand>
{
    public async ValueTask<Result> Handle(DeleteSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await subscriptions.FirstOrDefaultAsync(new Specification<WebhookSubscription>().Where(s => s.Id == command.Id).Tracked(), cancellationToken);
        if (subscription is null)
        {
            return Error.NotFound("webhooks.subscription_not_found");
        }

        subscriptions.Remove(subscription);
        await subscriptions.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Queues a <c>webhooks.ping</c> delivery to check the endpoint and the signature.</summary>
public sealed record PingSubscriptionCommand(Guid Id) : ICommand, IRequirePermission
{
    public string Permission => WebhooksPermissions.Subscriptions.Manage;
}

public sealed class PingSubscriptionHandler(
    IRepository<WebhookSubscription, Guid> subscriptions,
    IRepository<WebhookDelivery, Guid> deliveries,
    TimeProvider clock) : ICommandHandler<PingSubscriptionCommand>
{
    public async ValueTask<Result> Handle(PingSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (!await subscriptions.AnyAsync(new Specification<WebhookSubscription>().Where(s => s.Id == command.Id), cancellationToken))
        {
            return Error.NotFound("webhooks.subscription_not_found");
        }

        var now = clock.GetUtcNow();
        var id = NewId.Next();
        deliveries.Add(WebhookDelivery.Create(id, command.Id, WebhookEvents.Ping, WebhookPayloads.Create(id, WebhookEvents.Ping, now, new { message = "pong" }), now));
        await deliveries.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeliveryDto(
    Guid Id,
    string Event,
    string Status,
    int Attempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset NextAttemptAt,
    int? ResponseStatus,
    string? LastError);

public sealed record ListDeliveriesQuery(Guid SubscriptionId) : IQuery<IReadOnlyList<DeliveryDto>>, IRequirePermission
{
    public string Permission => WebhooksPermissions.Subscriptions.Manage;
}

public sealed class ListDeliveriesHandler(IRepository<WebhookDelivery, Guid> deliveries) : IQueryHandler<ListDeliveriesQuery, IReadOnlyList<DeliveryDto>>
{
    public async ValueTask<Result<IReadOnlyList<DeliveryDto>>> Handle(ListDeliveriesQuery query, CancellationToken cancellationToken) =>
        (await deliveries.ListAsync(
            new Specification<WebhookDelivery>().Where(d => d.SubscriptionId == query.SubscriptionId).OrderBy(d => d.CreatedAt, descending: true).Page(0, 100),
            cancellationToken))
        .Select(d => new DeliveryDto(d.Id, d.Event, d.Status, d.Attempts, d.CreatedAt, d.LastAttemptAt, d.NextAttemptAt, d.ResponseStatus, d.LastError))
        .ToList();
}

public sealed record RetryDeliveryCommand(Guid Id) : ICommand, IRequirePermission
{
    public string Permission => WebhooksPermissions.Subscriptions.Manage;
}

public sealed class RetryDeliveryHandler(IRepository<WebhookDelivery, Guid> deliveries, TimeProvider clock) : ICommandHandler<RetryDeliveryCommand>
{
    public async ValueTask<Result> Handle(RetryDeliveryCommand command, CancellationToken cancellationToken)
    {
        var delivery = await deliveries.FirstOrDefaultAsync(new Specification<WebhookDelivery>().Where(d => d.Id == command.Id).Tracked(), cancellationToken);
        if (delivery is null)
        {
            return Error.NotFound("webhooks.delivery_not_found");
        }

        delivery.Retry(clock.GetUtcNow());
        await deliveries.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class WebhooksPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(WebhooksPermissions.Subscriptions.Manage, WebhooksPermissions.Module, "permissions.webhooks.subscriptions.manage", "subscriptions"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("webhooks.subscriptions", WebhooksPermissions.Module, "nav.webhooks", "/webhooks", "webhook", WebhooksPermissions.Subscriptions.Manage, 850),
    ];
}
