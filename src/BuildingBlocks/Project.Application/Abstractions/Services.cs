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
/// The outbox message being dispatched. Delivery is at least once (a failing handler makes the whole message run
/// again), so consumers that write data derive their ids from <see cref="MessageId"/> to stay idempotent.
/// </summary>
public interface IEventContext
{
    /// <summary>Null outside of an outbox dispatch.</summary>
    Guid? MessageId { get; }

    /// <summary>The same id for the same message and discriminator, every time the message is retried.</summary>
    Guid? IdFor(string discriminator) => MessageId is { } id ? DeterministicId.From(id, discriminator) : null;
}

/// <summary>Name-based id (SHA-256 of a namespace id and a name, RFC 9562 version 8 layout).</summary>
public static class DeterministicId
{
    public static Guid From(Guid ns, string name)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData([.. ns.ToByteArray(), .. System.Text.Encoding.UTF8.GetBytes(name)]);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes.AsSpan(0, 16));
    }
}

/// <summary>
/// Sees every event dispatched by the outbox, whatever its type (webhooks, analytics, integrations). Runs after the
/// typed handlers, in the tenant of the event.
/// </summary>
public interface IDomainEventObserver
{
    Task OnEventAsync(IDomainEvent domainEvent, CancellationToken ct);
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
