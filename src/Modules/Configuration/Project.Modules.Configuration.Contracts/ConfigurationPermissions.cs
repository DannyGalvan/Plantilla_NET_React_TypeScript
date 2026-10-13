namespace Project.Modules.Configuration.Contracts;

/// <summary>Permission keys of the Configuration module. Platform administrators only.</summary>
public static class ConfigurationPermissions
{
    public const string Module = "configuration";

    public static class Platform
    {
        public const string Manage = "configuration.platform.manage";
    }
}
