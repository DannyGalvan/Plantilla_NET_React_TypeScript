namespace Project.Modules.Notifications.Contracts;

/// <summary>Permission keys of the Notifications module. Every signed-in user reads his own inbox without a permission.</summary>
public static class NotificationsPermissions
{
    public const string Module = "notifications";

    public static class Templates
    {
        public const string Manage = "notifications.templates.manage";
    }
}
