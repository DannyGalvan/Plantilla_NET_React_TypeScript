using Project.SharedKernel.Domain;

namespace Project.Modules.Notifications.Domain;

/// <summary>An entry of the in-app inbox of one user. When email was requested, the delivery goes through the outbox.</summary>
public sealed class Notification : AggregateRoot<Guid>, IMultiTenant
{
    private Notification()
    {
    }

    public Guid TenantId { get; set; }

    public Guid UserId { get; private set; }

    public string Template { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string? Link { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static Notification Create(Guid userId, string template, string title, string body, string? link, DateTimeOffset now) =>
        new()
        {
            Id = NewId.Next(),
            UserId = userId,
            Template = template,
            Title = Limit(title, 256),
            Body = Limit(body, 4000),
            Link = link,
            CreatedAt = now,
        };

    /// <summary>Queues the email copy of the notification (delivered by the outbox, retried on failure).</summary>
    public void RequestEmail(string to, string subject, string htmlBody) =>
        Raise(new NotificationEmailRequested(Id, to, subject, htmlBody));

    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;

    private static string Limit(string value, int length) => value.Length > length ? value[..length] : value;
}

public sealed record NotificationEmailRequested(Guid NotificationId, string To, string Subject, string HtmlBody) : IDomainEvent;

/// <summary>Text of a template changed by a tenant for one language.</summary>
public sealed class NotificationTemplateOverride : AuditableAggregateRoot, IMultiTenant
{
    public Guid TenantId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}
