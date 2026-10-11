# Adding a module

> Cómo agregar un módulo de negocio de extremo a extremo sin tocar código
> central.

## Backend (4 proyectos por módulo)

```
src/modules/Inventory/
  Project.Modules.Inventory.Domain/         # AggregateRoot<Guid>, value objects
  Project.Modules.Inventory.Application/    # Commands, Queries, validators
  Project.Modules.Inventory.Infrastructure/ # DbContext, repos, EF configs
  Project.Modules.Inventory.Contracts/      # Permissions + integration events
```

1. Define la entidad (hereda `AggregateRoot<Guid>`, marca
   `IMultiTenant` y `ISoftDeletable`).
2. En `Contracts/Permissions.cs` declara las constantes:
   ```csharp
   public static class InventoryPermissions
   {
       public const string ProductsRead   = "inventory.products.read";
       public const string ProductsCreate = "inventory.products.create";
       // ...
   }
   ```
3. En `Application` agrega los `ICommand`/`IQuery` con sus `IRequestHandler`
   (`sealed`). Validators con FluentValidation.
4. En `Infrastructure` configura el `IEntityTypeConfiguration` y registra
   el DbContext con `services.AddModuleDbContext<InventoryDbContext>("inventory", DataScope.Tenant)`.
5. En `InventoryModule : IModule`:
   ```csharp
   public void AddServices(...) {
       services.AddModuleDbContext<InventoryDbContext>("inventory", DataScope.Tenant);
       services.AddScoped<ICrudService<Product, Guid, ProductDto, ...>, CrudService<...>>();
       services.AddValidatorsFromAssembly(typeof(InventoryModule).Assembly);
   }
   public void MapEndpoints(IEndpointRouteBuilder endpoints) {
       endpoints.MapCrud<Product, Guid, ProductDto, CreateProductDto, UpdateProductDto>(
           "products", "inventory.products");
   }
   ```
6. Migración por proveedor:
   ```bash
   ./scripts/add-migration.sh Inventory InitialCreate
   ```
7. Registra el módulo en `src/Host/Project.Api/Program.cs`:
   ```csharp
   builder.AddModules(new LegacyModule(), new TenancyModule(),
                      new OrganizationModule(), new CatalogModule(),
                      new InventoryModule());
   ```

## Frontend (manifest-driven)

```
src/modules/inventory/
  module.ts                   # ModuleManifest default export
  api/inventory-api.ts        # Axios CRUD wrapper + zod schemas
  schemas/inventory.ts        # DTOs
  pages/InventoryPage.tsx     # Placeholder o con FormDialog
  i18n/{es,en}.json
```

1. Ejecuta:
   ```bash
   node scripts/new-module.mjs inventory
   ```
2. Rellena `module.ts` con `routes`, `nav`, `i18n`, `features`.
3. `main.tsx` ya recoge los manifests con `getAllManifests()`; no requiere
   tocar nada central.
4. Las pruebas E2E (Playwright) viven en `tests/e2e/inventory.spec.ts`.

## Reglas que aplican

- **Permissions**: `{module}.{resource}.{action}` en minúsculas. Se declaran
  como constantes en `Contracts/Permissions.cs`. Nunca se derivan del
  nombre del controller.
- **Fail-closed**: cada endpoint debe llevar `RequirePermission(...)` o
  `AllowWithoutTenant()` explícito. El
  `Project.ArchitectureTests.RequirePermissionConvention` lo verifica.
- **Borrado físico prohibido**: las entidades implementan `ISoftDeletable`
  y se eliminan vía el `SoftDeleteInterceptor` de `Project.Infrastructure`.
  El PR que agregue un módulo sin esto rompe la build.
- **Multi-tenant**: entidades de `DataScope.Tenant` implementan
  `IMultiTenant`. El filtro global en `ModuleDbContext` se aplica
  automáticamente.