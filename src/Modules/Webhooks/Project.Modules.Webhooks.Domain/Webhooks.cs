using Project.SharedKernel.Domain;

namespace Project.Modules.Webhooks.Domain;

/// <summary>An endpoint of the tenant that receives some events. The signing secret is stored encrypted.</summary>
public sealed class WebhookSubscription : AuditableAggregateRoot, IMultiTenant
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    /// <summary>Event names separated by commas, or <c>*</c> for every event.</summary>
    public string Events { get; set; } = "*";

    public string ProtectedSecret { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool Wants(string eventName) =>
        IsActive && (Events == "*" || Events.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains(eventName));
}

/// <summary>One attempt chain to deliver an event to a subscription, retried with exponential backoff.</summary>
public sealed class WebhookDelivery : AggregateRoot<Guid>, IMultiTenant
{
    public const int MaxAttempts = 8;

    private WebhookDelivery()
    {
    }

    public Guid TenantId { get; set; }

    public Guid SubscriptionId { get; private set; }

    public string Event { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public string Status { get; private set; } = Statuses.Pending;

    public int Attempts { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public DateTimeOffset? LastAttemptAt { get; private set; }

    public int? ResponseStatus { get; private set; }

    public string? LastError { get; private set; }

    public Guid? LockId { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public static WebhookDelivery Create(Guid id, Guid subscriptionId, string eventName, string payload, DateTimeOffset now) =>
        new()
        {
            Id = id,
            SubscriptionId = subscriptionId,
            Event = eventName,
            Payload = payload,
            CreatedAt = now,
            NextAttemptAt = now,
        };

    public void Succeeded(int status, DateTimeOffset now)
    {
        Attempts++;
        Status = Statuses.Delivered;
        ResponseStatus = status;
        LastError = null;
        LastAttemptAt = now;
        Release();
    }

    /// <summary>Waits 1, 2, 4, 8... minutes between attempts; gives up after <see cref="MaxAttempts"/>.</summary>
    public void Failed(int? status, string error, DateTimeOffset now)
    {
        Attempts++;
        ResponseStatus = status;
        LastError = error.Length > 1000 ? error[..1000] : error;
        LastAttemptAt = now;
        Status = Attempts >= MaxAttempts ? Statuses.Failed : Statuses.Pending;
        NextAttemptAt = now.AddMinutes(Math.Pow(2, Attempts - 1));
        Release();
    }

    /// <summary>Manual retry from the delivery log.</summary>
    public void Retry(DateTimeOffset now)
    {
        Status = Statuses.Pending;
        NextAttemptAt = now;
    }

    private void Release()
    {
        LockId = null;
        LockedUntil = null;
    }

    public static class Statuses
    {
        public const string Pending = "Pending";
        public const string Delivered = "Delivered";
        public const string Failed = "Failed";
    }
}
