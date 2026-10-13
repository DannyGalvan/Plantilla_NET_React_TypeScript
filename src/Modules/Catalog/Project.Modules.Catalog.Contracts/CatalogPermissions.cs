namespace Project.Modules.Catalog.Contracts;

/// <summary>Permission keys of the Catalog sample module.</summary>
public static class CatalogPermissions
{
    public const string Module = "catalog";

    public static class Products
    {
        public const string Prefix = "catalog.products";
        public const string Read = Prefix + ".read";
        public const string Update = Prefix + ".update";
    }

    public static class Categories
    {
        public const string Prefix = "catalog.categories";
        public const string Read = Prefix + ".read";
    }

    public static class Reports
    {
        public const string Read = "catalog.reports.read";
    }

    /// <summary>Generic CRUD actions appended to a prefix by <c>MapCrud</c>.</summary>
    public static readonly string[] CrudActions = ["read", "create", "update", "delete", "restore"];
}

/// <summary>Entity types that accept attachments (Files module).</summary>
public static class CatalogAttachments
{
    public const string Product = "catalog.product";
}
