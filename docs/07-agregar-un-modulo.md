# Agregar un módulo

**Para qué sirve este documento:** crear un módulo de negocio de punta a punta (backend y frontend).
**Tiempo estimado:** 15 minutos para el esqueleto.

## 1. Generar el esqueleto

```bash
node scripts/new-module.mjs Inventory Warehouse            # agrega --org-scoped si los datos pertenecen a una unidad
./scripts/add-migration.sh Inventory Initial
```

```powershell
node scripts/new-module.mjs Inventory Warehouse
./scripts/add-migration.ps1 -Module Inventory -Name Initial
```

El generador crea los cuatro proyectos, los agrega a `Project.slnx`, registra `new InventoryModule()` en
`src/Host/Project.Composition/AppModules.cs`, agrega las factorías de diseño en los tres proyectos de migraciones y crea
el módulo frontend `src/project.client/src/modules/inventory`. Al ejecutar la API ya existen `/api/v1/inventory/warehouses`
(CRUD completo), sus permisos `inventory.warehouses.*`, la entrada de menú y la página con la tabla.

## 2. Modelar el dominio

En `Inventory.Domain` deriva de `AuditableAggregateRoot` (Guid v7, auditoría, borrado lógico, concurrencia) e implementa
`IMultiTenant` siempre, e `IOrganizationScoped` si el dato pertenece a una empresa o marca. No asignes `TenantId` ni
`OrganizationUnitId` desde la petición: lo hace el pipeline de guardado.

## 3. Configuración EF

Agrega un `IEntityTypeConfiguration<T>` en `Inventory.Infrastructure` con longitudes máximas para todo `string`
indexado. El esquema (`inventory`) y los filtros globales se aplican solos.

## 4. Permisos y menú

Constantes en `Inventory.Contracts/InventoryPermissions.cs`; definición y navegación en el `IPermissionProvider` del
módulo. El rol `Administrator` de cada tenant recibe los permisos nuevos en la siguiente ejecución del migrador.

## 5. CRUD genérico o casos de uso propios

**CRUD genérico:** DTOs, un mapper Mapperly que implemente `ICrudMapper`, una `QueryPolicy` (allowlist de filtros y
orden) y validadores; luego `api.MapCrud<...>("inventory/warehouses", InventoryPermissions.Warehouses.Prefix)`.

**Caso de uso propio:**

```csharp
public sealed record TransferStockCommand(Guid From, Guid To, int Quantity) : ICommand<TransferResult>;

public sealed class TransferStockValidator : AbstractValidator<TransferStockCommand> { /* reglas */ }

public sealed class TransferStockHandler(IRepository<Warehouse, Guid> warehouses) : ICommandHandler<TransferStockCommand, TransferResult>
{
    public async ValueTask<Result<TransferResult>> Handle(TransferStockCommand command, CancellationToken ct) { /* ... */ }
}

// Infrastructure/Presentation
api.MapPost("inventory/transfers", async (TransferStockCommand c, ISender sender, CancellationToken ct) =>
        (await sender.Send(c, ct)).ToHttp())
    .RequirePermission("inventory.transfers.create");
```

## 6. Consultas de alto rendimiento

Registra `services.AddSqlExecutor<InventoryDb, InventoryDbContext>()` y usa `ISqlExecutor<InventoryDb>` con
`ScopedSql` para no saltarte el aislamiento (ejemplo completo en `Catalog`: `GetStockReportHandler`). Los
procedimientos almacenados se crean con una migración por motor (ver [acceso a datos](guias/acceso-a-datos.md)).

## 7. Settings y feature flags

Declara `public sealed class InventorySettings : ISettingsSection` y regístrala con
`services.AddSettingsSection<InventorySettings>()`; aparece sola en la pantalla de configuración. Oculta endpoints con
`.RequireFeature("inventory.advanced")`.

## 8. Eventos

Emite eventos con `Raise(new StockLow(...))` dentro del agregado; se guardan en el outbox y se publican a los
`IDomainEventHandler<StockLow>` de cualquier módulo (definiendo el evento en `Contracts` si otro módulo lo consume).

## 9. Pruebas

Las pruebas funcionales de aislamiento existentes sirven de modelo (`tests/Project.Api.FunctionalTests`). Agrega
pruebas unitarias del dominio en `tests/Project.UnitTests`.

## 10. Frontend

Edita `src/project.client/src/modules/inventory`: esquema zod en `api.ts`, formularios con `FormField` y
`applyServerErrors`, textos en `i18n.ts` (es y en). Las rutas declaran `handle.permission`.

## 11. Verificar

```bash
dotnet build Project.slnx && dotnet test Project.slnx
cd src/project.client && pnpm verify
```

## Errores frecuentes

| Síntoma | Causa |
|---|---|
| La API no arranca: "Endpoints without an explicit security declaration" | Falta `RequirePermission`, `RequireAuthenticatedUser` o `AllowAnonymous` |
| 400 "Field 'x' cannot be filtered" | El campo no está en la `QueryPolicy` |
| El menú no muestra el módulo | El usuario no tiene el permiso de lectura o falta la entrada en `GetNavigation` |
| `has-pending-model-changes` falla en CI | Cambiaste el modelo sin generar la migración en los tres motores |
