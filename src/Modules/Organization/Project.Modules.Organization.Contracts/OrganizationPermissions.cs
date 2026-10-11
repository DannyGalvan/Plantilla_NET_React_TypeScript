namespace Project.Modules.Organization.Contracts;

/// <summary>Permission keys of the Organization module.</summary>
public static class OrganizationPermissions
{
    public const string Module = "organization";

    public static class Units
    {
        public const string Prefix = "organization.units";
        public const string Read = Prefix + ".read";
        public const string Create = Prefix + ".create";
        public const string Update = Prefix + ".update";
        public const string Delete = Prefix + ".delete";
        public const string Restore = Prefix + ".restore";
    }

    public static class UnitTypes
    {
        public const string Prefix = "organization.unit-types";
        public const string Read = Prefix + ".read";
        public const string Create = Prefix + ".create";
        public const string Update = Prefix + ".update";
        public const string Delete = Prefix + ".delete";
        public const string Restore = Prefix + ".restore";
    }

    public static class Assignments
    {
        public const string Manage = "organization.assignments.manage";
    }

    /// <summary>Sees every unit of the tenant (no organization filter) unless a unit is selected.</summary>
    public const string ScopeAll = "organization.scope.all";
}
