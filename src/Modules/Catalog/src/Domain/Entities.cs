using Project.SharedKernel.Domain;

namespace Project.Modules.Catalog.Domain;

/// <summary>
/// Catalog product. Implements <see cref="IMultiTenant"/> so the global filter
/// in <see cref="Project.Infrastructure.Persistence.ModuleDbContext"/> applies
/// when tenancy is active.
/// </summary>
public sealed class Product : AggregateRoot<Guid>, IMultiTenant
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryId { get; set; }
    public Guid TenantId { get; set; }
}

public sealed class Category : AggregateRoot<Guid>, IMultiTenant
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid TenantId { get; set; }
}