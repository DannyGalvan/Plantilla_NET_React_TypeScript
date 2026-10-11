namespace Project.Modules.Identity.Contracts;

/// <summary>Permission keys of the Identity module.</summary>
public static class IdentityPermissions
{
    public const string Module = "identity";

    public static class Users
    {
        public const string Prefix = "identity.users";
        public const string Read = Prefix + ".read";
        public const string Create = Prefix + ".create";
        public const string Update = Prefix + ".update";
        public const string Delete = Prefix + ".delete";
        public const string Restore = Prefix + ".restore";
        public const string ResetPassword = Prefix + ".reset-password";
    }

    public static class Roles
    {
        public const string Prefix = "identity.roles";
        public const string Read = Prefix + ".read";
        public const string Create = Prefix + ".create";
        public const string Update = Prefix + ".update";
        public const string Delete = Prefix + ".delete";
        public const string Restore = Prefix + ".restore";
        public const string AssignPermissions = Prefix + ".assign-permissions";
    }
}

/// <summary>Well-known role names created by the seeders.</summary>
public static class IdentityRoles
{
    /// <summary>Platform role (no tenant): every permission in every tenant.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Tenant role: every permission except platform administration.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Tenant role: read access to business modules.</summary>
    public const string User = "User";
}
