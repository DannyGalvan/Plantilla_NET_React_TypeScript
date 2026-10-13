using Project.SharedKernel.Domain;

namespace Project.Modules.Catalog.Domain;

/// <summary>Product category, shared by every unit of the tenant.</summary>
public sealed class Category : AuditableAggregateRoot, IMultiTenant
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

/// <summary>Product owned by an organization unit (for example a brand): users only see products of their units.</summary>
public sealed class Product : AuditableAggregateRoot, IMultiTenant, IOrganizationScoped
{
    public Guid TenantId { get; set; }

    public Guid OrganizationUnitId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}
