using FluentValidation;
using Project.Application.Abstractions;
using Project.Application.Crud;
using Project.Application.Querying;
using Project.Application.Security;
using Project.Modules.Catalog.Contracts;
using Project.Modules.Catalog.Domain;
using Riok.Mapperly.Abstractions;

namespace Project.Modules.Catalog.Application;

// ----- Categories -----

public sealed record CategoryDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt, Guid ConcurrencyStamp);

public sealed record CreateCategoryRequest(string Name, string? Description);

public sealed record UpdateCategoryRequest(string Name, string? Description, Guid? ConcurrencyStamp) : IConcurrencyAware;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public sealed partial class CategoryMapper : ICrudMapper<Category, CategoryDto, CreateCategoryRequest, UpdateCategoryRequest>
{
    public partial CategoryDto ToDto(Category entity);

    public partial IQueryable<CategoryDto> ProjectToDto(IQueryable<Category> query);

    public partial Category ToEntity(CreateCategoryRequest input);

    [MapperIgnoreTarget(nameof(Category.ConcurrencyStamp))]
    public partial void Apply(UpdateCategoryRequest input, [MappingTarget] Category entity);

    public partial UpdateCategoryRequest ToUpdate(Category entity);
}

public sealed class CategoryQueryPolicy : QueryPolicy<Category>
{
    public CategoryQueryPolicy()
    {
        Field(c => c.Name).Field(c => c.CreatedAt).Filterable(c => c.Description);
        DefaultOrder(c => c.Name);
    }
}

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(128);
        RuleFor(r => r.Description).MaximumLength(512);
    }
}

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(128);
        RuleFor(r => r.Description).MaximumLength(512);
    }
}

// ----- Products -----

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    Guid OrganizationUnitId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid ConcurrencyStamp);

/// <summary>The organization unit is optional: when omitted, the active unit of the user is used.</summary>
public sealed record CreateProductRequest(string Sku, string Name, string? Description, decimal Price, int Stock, Guid CategoryId, Guid? OrganizationUnitId);

public sealed record UpdateProductRequest(string Name, string? Description, decimal Price, int Stock, Guid CategoryId, bool IsActive, Guid? ConcurrencyStamp)
    : IConcurrencyAware;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public sealed partial class ProductMapper : ICrudMapper<Product, ProductDto, CreateProductRequest, UpdateProductRequest>
{
    public partial ProductDto ToDto(Product entity);

    public partial IQueryable<ProductDto> ProjectToDto(IQueryable<Product> query);

    public Product ToEntity(CreateProductRequest input)
    {
        var product = MapNew(input);
        product.Sku = input.Sku.Trim().ToUpperInvariant();
        product.OrganizationUnitId = input.OrganizationUnitId ?? Guid.Empty;
        return product;
    }

    [MapperIgnoreTarget(nameof(Product.ConcurrencyStamp))]
    public partial void Apply(UpdateProductRequest input, [MappingTarget] Product entity);

    public partial UpdateProductRequest ToUpdate(Product entity);

    [MapperIgnoreTarget(nameof(Product.OrganizationUnitId))]
    private partial Product MapNew(CreateProductRequest input);
}

public sealed class ProductQueryPolicy : QueryPolicy<Product>
{
    public ProductQueryPolicy()
    {
        Field(p => p.Sku).Field(p => p.Name).Field(p => p.Price).Field(p => p.Stock).Field(p => p.IsActive).Field(p => p.CreatedAt);
        Filterable(p => p.CategoryId).Filterable(p => p.OrganizationUnitId);
        DefaultOrder(p => p.Name);
    }
}

public sealed class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator(IRepository<Category, Guid> categories)
    {
        RuleFor(r => r.Sku).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9._-]+$");
        RuleFor(r => r.Name).NotEmpty().MaximumLength(128);
        RuleFor(r => r.Description).MaximumLength(1024);
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 4, ignoreTrailingZeros: true);
        RuleFor(r => r.Stock).GreaterThanOrEqualTo(0);
        RuleFor(r => r.CategoryId).NotEmpty().MustAsync((id, ct) => categories.AnyAsync(new Specification<Category>().Where(c => c.Id == id), ct))
            .WithMessage("The category does not exist.");
    }
}

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator(IRepository<Category, Guid> categories)
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(128);
        RuleFor(r => r.Description).MaximumLength(1024);
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 4, ignoreTrailingZeros: true);
        RuleFor(r => r.Stock).GreaterThanOrEqualTo(0);
        RuleFor(r => r.CategoryId).NotEmpty().MustAsync((id, ct) => categories.AnyAsync(new Specification<Category>().Where(c => c.Id == id), ct))
            .WithMessage("The category does not exist.");
    }
}

/// <summary>Permissions and navigation of the Catalog module.</summary>
public sealed class CatalogPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
        CatalogPermissions.CrudActions.Select(a => new PermissionDefinition($"{CatalogPermissions.Products.Prefix}.{a}", CatalogPermissions.Module, $"permissions.catalog.products.{a}", "products"))
            .Concat(CatalogPermissions.CrudActions.Select(a => new PermissionDefinition($"{CatalogPermissions.Categories.Prefix}.{a}", CatalogPermissions.Module, $"permissions.catalog.categories.{a}", "categories")))
            .Append(new PermissionDefinition(CatalogPermissions.Reports.Read, CatalogPermissions.Module, "permissions.catalog.reports.read", "reports"));

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("catalog.products", CatalogPermissions.Module, "nav.products", "/catalog/products", "package", CatalogPermissions.Products.Read, 100),
        new("catalog.categories", CatalogPermissions.Module, "nav.categories", "/catalog/categories", "tags", CatalogPermissions.Categories.Read, 110),
        new("catalog.reports", CatalogPermissions.Module, "nav.stockReport", "/catalog/reports/stock", "bar-chart-3", CatalogPermissions.Reports.Read, 120),
    ];
}
