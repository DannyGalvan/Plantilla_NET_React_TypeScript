using Microsoft.Extensions.DependencyInjection;

namespace Project.Application.Notifications;

/// <summary>
/// A notification for one or more users of the current tenant. <see cref="Data"/> fills the placeholders of the
/// template (<c>{{name}}</c>); <see cref="Link"/> is the page the user opens from the inbox.
/// </summary>
public sealed record Notify(
    string Template,
    IReadOnlyCollection<Guid> UserIds,
    IReadOnlyDictionary<string, string>? Data = null,
    string? Link = null,
    bool Email = false);

/// <summary>
/// Sends notifications: an entry in the in-app inbox of every recipient and, optionally, an email delivered by the
/// outbox (retried, safe with several instances). Call it from a domain event handler so the notification is not lost
/// when the business transaction commits. Does nothing when the Notifications module is not loaded.
/// </summary>
public interface INotificationSender
{
    Task SendAsync(Notify notification, CancellationToken ct);
}

/// <summary>Text of a template in one language. Placeholders use <c>{{name}}</c>.</summary>
public sealed record LocalizedTemplate(string Subject, string Body);

/// <summary>
/// A template declared by the module that sends it. Tenants may override subject and body per language without
/// code; <see cref="Placeholders"/> documents the values the sender provides.
/// </summary>
public sealed record NotificationTemplate(
    string Key,
    string Module,
    IReadOnlyDictionary<string, LocalizedTemplate> Languages,
    IReadOnlyList<string> Placeholders);

public static class NotificationTemplateExtensions
{
    /// <summary>
    /// Declares a template:
    /// <code>
    /// services.AddNotificationTemplate(new("hr.leave.approved", "hr", new Dictionary&lt;string, LocalizedTemplate&gt;
    /// {
    ///     ["es"] = new("Vacaciones aprobadas", "{{approver}} aprobó tus vacaciones del {{from}} al {{to}}."),
    ///     ["en"] = new("Leave approved", "{{approver}} approved your leave from {{from}} to {{to}}."),
    /// }, ["approver", "from", "to"]));
    /// </code>
    /// </summary>
    public static IServiceCollection AddNotificationTemplate(this IServiceCollection services, NotificationTemplate template) =>
        services.AddSingleton(template);
}
