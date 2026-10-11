using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Crud;
using Project.Application.Querying;
using Project.Modules.Catalog.Application;
using Project.Modules.Catalog.Domain;
using Project.Web;
using Project.Web.Crud;

namespace Project.Modules.Catalog;

/// <summary>
/// Catalog sample module. Registers CRUD services for <see cref="Product"/>
/// and <see cref="Category"/>, discovers FluentValidation validators by
/// assembly scan, and wires the filter operators once.
/// </summary>
public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";
    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProjector<Product, ProductDto>, ProductMapper>();
        services.AddScoped<IMapperTo<CreateProductDto, Product>>(sp => sp.GetRequiredService<ProductMapper>());
        services.AddScoped<IMapperTo<UpdateProductDto, Product>>(sp => sp.GetRequiredService<ProductMapper>());
        services.AddScoped<IQueryPolicy<Product>, ProductQueryPolicy>();

        services.AddScoped<IProjector<Category, CategoryDto>, CategoryMapper>();
        services.AddScoped<IMapperTo<CreateCategoryDto, Category>>(sp => sp.GetRequiredService<CategoryMapper>());
        services.AddScoped<IMapperTo<UpdateCategoryDto, Category>>(sp => sp.GetRequiredService<CategoryMapper>());
        services.AddScoped<IQueryPolicy<Category>, CategoryQueryPolicy>();

        services.AddScoped<FilterParser<Product>>(sp => new FilterParser<Product>(DefaultOperators()));
        services.AddScoped<FilterParser<Category>>(sp => new FilterParser<Category>(DefaultOperators()));
        services.AddSingleton<SortParser<Product>>();
        services.AddSingleton<SortParser<Category>>();

        services.AddScoped<ICrudService<Product, Guid, ProductDto, CreateProductDto, UpdateProductDto>,
            CrudService<Product, Guid, ProductDto, CreateProductDto, UpdateProductDto>>();
        services.AddScoped<ICrudService<Category, Guid, CategoryDto, CreateCategoryDto, UpdateCategoryDto>,
            CrudService<Category, Guid, CategoryDto, CreateCategoryDto, UpdateCategoryDto>>();

        services.AddValidatorsFromAssembly(typeof(CatalogModule).Assembly);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCrud<Product, Guid, ProductDto, CreateProductDto, UpdateProductDto>("products", "catalog.products");
        endpoints.MapCrud<Category, Guid, CategoryDto, CreateCategoryDto, UpdateCategoryDto>("categories", "catalog.categories");
    }

    private static IFilterOperator[] DefaultOperators() =>
        new IFilterOperator[]
        {
            new EqualOperator(),
            new NotEqualOperator(),
            new GreaterThanOperator(),
            new GreaterThanOrEqualOperator(),
            new LessThanOperator(),
            new LessThanOrEqualOperator(),
            new ContainsOperator(),
            new StartsWithOperator(),
            new IsNullOperator(),
        };
}