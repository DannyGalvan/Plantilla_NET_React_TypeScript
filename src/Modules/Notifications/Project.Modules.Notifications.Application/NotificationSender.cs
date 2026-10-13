using System.Net;
using System.Text.RegularExpressions;
using Project.Application.Abstractions;
using Project.Application.Notifications;
using Project.Application.Settings;
using Project.Modules.Identity.Contracts;
using Project.Modules.Notifications.Domain;

namespace Project.Modules.Notifications.Application;

/// <summary>Templates declared by the modules plus the overrides of the current tenant.</summary>
public sealed partial class TemplateRenderer(IEnumerable<NotificationTemplate> templates, IRepository<NotificationTemplateOverride, Guid> overrides)
{
    public IReadOnlyList<NotificationTemplate> Declared { get; } = templates.OrderBy(t => t.Key, StringComparer.Ordinal).ToList();

    public async Task<LocalizedTemplate> ResolveAsync(string key, string language, CancellationToken ct)
    {
        var custom = await overrides.FirstOrDefaultAsync(
            new Specification<NotificationTemplateOverride>().Where(o => o.Key == key && o.Language == language), ct);
        if (custom is not null)
        {
            return new LocalizedTemplate(custom.Subject, custom.Body);
        }

        var template = Declared.FirstOrDefault(t => t.Key == key)
            ?? throw new InvalidOperationException($"The notification template '{key}' is not declared.");
        return template.Languages.TryGetValue(language, out var text) ? text : template.Languages.Values.First();
    }

    /// <summary>Replaces <c>{{name}}</c> placeholders; unknown ones become empty.</summary>
    public static string Render(string text, IReadOnlyDictionary<string, string>? data) =>
        Placeholder().Replace(text, match => data is not null && data.TryGetValue(match.Groups[1].Value, out var value) ? value : string.Empty);

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_.-]+)\s*\}\}")]
    private static partial Regex Placeholder();
}

/// <summary>Writes the inbox entries and queues the emails in the same transaction (outbox).</summary>
public sealed class NotificationSender(
    IRepository<Notification, Guid> notifications,
    TemplateRenderer templates,
    IUserDirectory users,
    ISettingsProvider settings,
    TimeProvider clock) : INotificationSender
{
    public async Task SendAsync(Notify notification, CancellationToken ct)
    {
        var recipients = await users.FindAsync(notification.UserIds.Distinct().ToList(), ct);
        if (recipients.Count == 0)
        {
            return;
        }

        var localization = await settings.GetAsync<LocalizationSettings>(ct);
        var branding = await settings.GetAsync<BrandingSettings>(ct);
        var text = await templates.ResolveAsync(notification.Template, Language(localization.Locale), ct);
        var title = TemplateRenderer.Render(text.Subject, notification.Data);
        var body = TemplateRenderer.Render(text.Body, notification.Data);
        var now = clock.GetUtcNow();

        foreach (var user in recipients)
        {
            var entry = Notification.Create(user.Id, notification.Template, title, body, notification.Link, now);
            if (notification.Email && !string.IsNullOrWhiteSpace(user.Email))
            {
                entry.RequestEmail(user.Email, title, EmailLayout.Render(branding.DisplayName, title, body));
            }

            notifications.Add(entry);
        }

        await notifications.UnitOfWork.SaveChangesAsync(ct);
    }

    private static string Language(string locale) => locale.Split('-', '_')[0].ToLowerInvariant();
}

/// <summary>Minimal accessible HTML for notification emails. Every value is encoded (no HTML injection from data).</summary>
public static class EmailLayout
{
    public static string Render(string brand, string title, string body)
    {
        var paragraphs = string.Join(
            string.Empty,
            body.Split('\n').Select(line => $"<p style=\"margin:0 0 12px\">{WebUtility.HtmlEncode(line.TrimEnd('\r'))}</p>"));
        return $"""
            <!doctype html>
            <html><body style="font-family:Arial,Helvetica,sans-serif;color:#1f2937;line-height:1.5">
            <p style="font-size:12px;color:#6b7280">{WebUtility.HtmlEncode(brand)}</p>
            <h1 style="font-size:18px">{WebUtility.HtmlEncode(title)}</h1>
            {paragraphs}
            </body></html>
            """;
    }
}

/// <summary>Delivers the email copy of a notification. A failure is retried by the outbox.</summary>
public sealed class NotificationEmailHandler(IEmailSender email) : IDomainEventHandler<NotificationEmailRequested>
{
    public Task HandleAsync(NotificationEmailRequested domainEvent, CancellationToken ct) =>
        email.SendAsync(domainEvent.To, domainEvent.Subject, domainEvent.HtmlBody, ct);
}
