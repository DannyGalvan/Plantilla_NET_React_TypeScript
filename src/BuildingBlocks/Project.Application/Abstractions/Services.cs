using Project.SharedKernel.Domain;

namespace Project.Application.Abstractions;

/// <summary>
/// Facade over HybridCache (memory + optional Redis) with tag invalidation.
/// Keys and tags are prefixed with <c>t:{tenantId}:</c> automatically when a tenant is active.
/// </summary>
public interface ICacheService
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        IEnumerable<string>? tags = null,
        TimeSpan? expiration = null,
        CancellationToken ct = default);

    ValueTask RemoveAsync(string key, CancellationToken ct = default);

    ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default);
}

/// <summary>Handles a domain event dispatched from the outbox.</summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);
}

/// <summary>
/// Publishes domain and integration events. The default implementation dispatches in process;
/// a broker (RabbitMQ, Azure Service Bus) can replace it without touching the modules.
/// </summary>
public interface IEventBus
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct);
}

/// <summary>Outgoing email abstraction (SMTP in infrastructure, a log sink in development).</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct);
}
