namespace Project.Modules.Tenancy.Contracts;

/// <summary>Permission keys of the Tenancy module.</summary>
public static class TenancyPermissions
{
    public const string Module = "tenancy";

    public static class Tenants
    {
        public const string Prefix = "tenancy.tenants";
        public const string Read = Prefix + ".read";
        public const string Manage = Prefix + ".manage";
    }

    public static class Settings
    {
        public const string Prefix = "tenancy.settings";
        public const string Read = Prefix + ".read";
        public const string Manage = Prefix + ".manage";
    }

    public static class Features
    {
        /// <summary>Feature flag that shows the platform administration UI (tenants, domains, plans).</summary>
        public const string PlatformAdmin = "platform.admin";
    }
}
