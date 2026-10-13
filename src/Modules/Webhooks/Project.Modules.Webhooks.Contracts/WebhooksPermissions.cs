namespace Project.Modules.Webhooks.Contracts;

/// <summary>Permission keys of the Webhooks module.</summary>
public static class WebhooksPermissions
{
    public const string Module = "webhooks";

    public static class Subscriptions
    {
        public const string Manage = "webhooks.subscriptions.manage";
    }
}

/// <summary>Event sent by "test" to check an endpoint.</summary>
public static class WebhookEvents
{
    public const string Ping = "webhooks.ping";
}
