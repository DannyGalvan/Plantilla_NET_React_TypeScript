# 07 · Agregar un módulo

> Backend y frontend, de punta a punta. Asume que ya tienes la app
> corriendo (perfil `simple` o `erp`) y vas a agregar `Inventory` como
> ejemplo. Adapta los nombres al módulo real.

## 1. Scaffolding backend

```bash
mkdir -p src/Modules/Inventory/src/{Domain,Application,Infrastructure}
mkdir -p src/Modules/Inventory.Contracts
```

Crea los 4 csproj con `dotnet new classlib`:

```bash
cd src/Modules/Inventory
dotnet new classlib -n Project.Modules.Inventory.Domain --framework net10.0 -o src/Domain
dotnet new classlib -n Project.Modules.Inventory.Application --framework net10.0 -o src/Application
dotnet new classlib -n Project.Modules.Inventory.Infrastructure --framework net10.0 -o src/Infrastructure
dotnet new classlib -n Project.Modules.Inventory.Contracts --framework net10.0 -o ../Project.Modules.Inventory.Contracts
```

## 2. Configurar referencias

```xml
<!-- Project.Modules.Inventory.Domain.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\..\BuildingBlocks\Project.SharedKernel\Project.SharedKernel.csproj" />
</ItemGroup>

<!-- Project.Modules.Inventory.Application.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\..\BuildingBlocks\Project.SharedKernel\Project.SharedKernel.csproj" />
  <ProjectReference Include="..\..\..\BuildingBlocks\Project.Application\Project.Application.csproj" />
  <ProjectReference Include="..\Project.Modules.Inventory.Domain\Project.Modules.Inventory.Domain.csproj" />
  <ProjectReference Include="..\Project.Modules.Inventory.Contracts\Project.Modules.Inventory.Contracts.csproj" />
</ItemGroup>

<!-- Project.Modules.Inventory.Infrastructure.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\..\BuildingBlocks\Project.Infrastructure\Project.Infrastructure.csproj" />
  <ProjectReference Include="..\Project.Modules.Inventory.Application\Project.Modules.Inventory.Application.csproj" />
  <ProjectReference Include="..\Project.Modules.Inventory.Domain\Project.Modules.Inventory.Domain.csproj" />
</ItemGroup>
```

## 3. Entidad

```csharp
// src/Modules/Inventory/src/Domain/Product.cs
namespace Project.Modules.Inventory.Domain;

public sealed class Product : AggregateRoot<Guid>, IMultiTenant, ISoftDeletable
{
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
```

> `IMultiTenant` + `ISoftDeletable` son obligatorios: el filtro global y el
> interceptor los respetan automáticamente.

## 4. Permisos (en `Contracts/Permissions.cs`)

```csharp
namespace Project.Modules.Inventory.Contracts;

public static class InventoryPermissions
{
    public const string ProductsRead   = "inventory.products.read";
    public const string ProductsCreate = "inventory.products.create";
    public const string ProductsUpdate = "inventory.products.update";
    public const string ProductsDelete = "inventory.products.delete";
}
```

Regla: `{module}.{resource}.{action}` en minúsculas. Nunca derivados del
nombre del controller.

## 5. CRUD genérico (Application + Infrastructure)

```csharp
// Application/Dtos.cs
public sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price, int Stock);
public sealed record CreateProductDto(string Sku, string Name, decimal Price, int Stock);
public sealed record UpdateProductDto(string? Name, decimal? Price, int? Stock, Guid? ConcurrencyStamp);
```

```csharp
// Infrastructure/Persistence/ProductConfiguration.cs
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products", "inventory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.Stock);
        b.Property(x => x.TenantId);
        b.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
    }
}
```

```csharp
// Infrastructure/InventoryDbContext.cs
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantContext tenant, IOrganizationContext org)
    : ModuleDbContext(options, tenant, org) { }

protected override void OnModelCreating(ModelBuilder b)
{
    base.OnModelCreating(b);
    b.ApplyConfigurationsFromAssembly(typeof(ProductConfiguration).Assembly);
}
```

## 6. MapCrud en Infrastructure o en un módulo host

```csharp
// src/Modules/Inventory/InventoryModule.cs
public sealed class InventoryModule : IModule
{
    public string Name => "Inventory";
    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<InventoryDbContext>("inventory", DataScope.Tenant);
        services.AddScoped<ICrudService<Product, Guid, ProductDto, CreateProductDto, UpdateProductDto>,
            CrudService<Product, Guid, ProductDto, CreateProductDto, UpdateProductDto>>();
        services.AddValidatorsFromAssembly(typeof(InventoryModule).Assembly);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCrud<Product, Guid, ProductDto, CreateProductDto, UpdateProductDto>(
            "products", "inventory.products");
    }
}
```

## 7. Registrar en `Program.cs`

```csharp
builder.AddModules(
    new LegacyModule(),
    new TenancyModule(),
    new OrganizationModule(),
    new CatalogModule(),
    new InventoryModule());
```

## 8. Migración por proveedor

```bash
./scripts/add-migration.sh Inventory InitialCreate
```

Genera la migración en los 3 proyectos `Project.Migrations.{Provider}`.

## 9. Scaffolding frontend

```bash
node scripts/new-module.mjs inventory
```

Crea `src/modules/inventory/` con module.ts, api/, schemas/, pages/, i18n/.

## 10. Scaffolding de tests

```bash
mkdir -p tests/Project.Modules.Inventory.UnitTests
# agregar el proyecto a la sln y crear:
# - tests de handler: fakes de IRepository, asserts sobre el Result<T>.
# - tests de MapCrud: WebApplicationFactory + Testcontainers con el módulo
#   registrado, asserts de permisos (RequireOperation/AllowAnonymous) y de
#   paginación.
```

## 11. Tests de aislamiento (Fase 5)

```bash
mkdir -p tests/Project.IsolationTests
# matrix TEST_DB_PROVIDERS=SqlServer,PostgreSql,MySql x TEST_TENANCY_MODES=None,SharedDatabase,DatabasePerTenant,Hybrid
```

`Project.IsolationTests` crea dos tenants y dos marcas dentro de uno;
verifica que ningún endpoint CRUD ni query Dapper devuelve, cuenta,
actualiza o borra filas del otro tenant ni de otra unidad no accesible.

## 12. Verificar

```bash
dotnet build Project.sln --configuration Release -warnaserror
dotnet test  Project.sln --no-build --configuration Release
cd project.client && pnpm verify
```

## Errores frecuentes al crear un módulo

- **Entity sin `IMultiTenant`** rompe el filtro global en
  `SharedDatabase`/`Hybrid`. El architecture test lo detecta.
- **Entity sin `ISoftDeletable`** permite el borrado físico. La
  prohibición está en Fase 5; mientras tanto, asegúrate manualmente.
- **`Permission` derivado del nombre del controller** cambia al renombrar
  y rompe a los usuarios. Declara siempre constantes en
  `Contracts/Permissions.cs`.
- **`MapCrud` sin `Permission`** arranca el fail-closed en CI (cuando
  Fase 5 lo enchufe). Por ahora, asegúrate de pasar el
  `permissionPrefix`.
- **`IRepository<T>.Remove(...)`** en lugar de un endpoint que marque
  `IsDeleted=true`. El `SoftDeleteInterceptor` convierte los
  `ISoftDeletable`, pero los no-`ISoftDeletable` se borran físicamente.