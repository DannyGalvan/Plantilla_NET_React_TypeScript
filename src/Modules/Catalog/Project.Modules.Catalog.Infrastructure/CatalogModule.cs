using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Catalog.Application;
using Project.Modules.Catalog.Contracts;
using Project.Modules.Catalog.Domain;
using Project.Web.Crud;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Catalog.Infrastructure;

/// <summary>Tenant database of the Catalog sample module.</summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => CatalogTables.Schema;

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable(CatalogTables.Categories);
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(128).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(512);
        builder.Property(c => c.CreatedBy).HasMaxLength(64);
        builder.Property(c => c.UpdatedBy).HasMaxLength(64);
        builder.Property(c => c.DeletedBy).HasMaxLength(64);
        builder.HasIndex(c => new { c.TenantId, c.Name });
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(CatalogTables.Products);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Sku).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1024);
        builder.Property(p => p.CreatedBy).HasMaxLength(64);
        builder.Property(p => p.UpdatedBy).HasMaxLength(64);
        builder.Property(p => p.DeletedBy).HasMaxLength(64);
        builder.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.Name });
        builder.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Development sample data for every tenant (enabled with <c>Seed:SampleData=true</c>).</summary>
internal sealed class CatalogSampleSeeder(IConfiguration configuration) : IDataSeeder
{
    public int Order => 100;

    public SeederScope Scope => SeederScope.PerTenant;

    public async Task SeedAsync(IServiceProvider services, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("Seed:SampleData"))
        {
            return;
        }

        var db = services.GetRequiredService<CatalogDbContext>();
        if (await db.Categories.IgnoreQueryFilters([QueryFilters.SoftDelete]).AnyAsync(ct))
        {
            return;
        }

        var hardware = new Category { Name = "Hardware", Description = "Physical devices" };
        var software = new Category { Name = "Software", Description = "Licenses and subscriptions" };
        db.Categories.AddRange(hardware, software);
        db.Products.AddRange(
            new Product { Sku = "LAP-001", Name = "Laptop 14", Price = 1199.90m, Stock = 12, CategoryId = hardware.Id },
            new Product { Sku = "MON-027", Name = "Monitor 27", Price = 329.00m, Stock = 3, CategoryId = hardware.Id },
            new Product { Sku = "KEY-100", Name = "Mechanical keyboard", Price = 89.50m, Stock = 40, CategoryId = hardware.Id },
            new Product { Sku = "ERP-STD", Name = "ERP standard license", Price = 49.00m, Stock = 2, CategoryId = software.Id });
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Sample business module: two CRUD resources with zero handlers (generic engine), a Dapper report and a stored
/// procedure. Opt-in: it loads only with <c>Modules:Enabled: ["Catalog"]</c> (on in Development). Use it as a reference
/// or remove it with <c>node scripts/remove-module.mjs Catalog</c>.
/// </summary>
public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public DataScope DataScope => DataScope.Tenant;

    public bool EnabledByDefault => false;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CatalogDbContext>(Name, DataScope);
        services.AddRepositoriesFor<CatalogDbContext>();
        services.AddSqlExecutor<CatalogDb, CatalogDbContext>();
        services.AddModuleConventions(typeof(CatalogPermissionProvider).Assembly);
        services.AddScoped<IDataSeeder, CatalogSampleSeeder>();
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        api.MapCrud<Guid, CategoryDto, CreateCategoryRequest, UpdateCategoryRequest>("catalog/categories", CatalogPermissions.Categories.Prefix);
        api.MapCrud<Guid, ProductDto, CreateProductRequest, UpdateProductRequest>("catalog/products", CatalogPermissions.Products.Prefix);

        var reports = api.MapGroup("catalog/reports").WithTags("catalog-reports");
        reports.MapGet("/stock", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetStockReportQuery(), ct)).ToHttp())
            .RequirePermission(CatalogPermissions.Reports.Read)
            .WithName("GetStockReport");
        reports.MapGet("/low-stock", async (int? threshold, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetLowStockQuery(threshold ?? 5), ct)).ToHttp())
            .RequirePermission(CatalogPermissions.Reports.Read)
            .WithName("GetLowStock");
    }
}
