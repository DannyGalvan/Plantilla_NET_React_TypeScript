using Microsoft.Extensions.DependencyInjection;
using Project.SharedKernel.Domain;

namespace Project.Application.Webhooks;

/// <summary>An event that tenants may receive in their own systems through webhooks.</summary>
public sealed record WebhookEventDefinition(string Name, Type EventType, string? Description);

public static class WebhookEventExtensions
{
    /// <summary>
    /// Publishes a domain or integration event to webhooks under a stable public name:
    /// <c>services.AddWebhookEvent&lt;OrderShipped&gt;("sales.order.shipped")</c>. The payload is the event serialized
    /// as JSON, so only expose events whose fields may leave the application. Ignored when the Webhooks module is off.
    /// </summary>
    public static IServiceCollection AddWebhookEvent<TEvent>(this IServiceCollection services, string name, string? description = null)
        where TEvent : IDomainEvent =>
        services.AddSingleton(new WebhookEventDefinition(name, typeof(TEvent), description));
}
