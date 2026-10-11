# FAQ

## ¿Por qué monolito modular y no microservicios?

Una plantilla empresarial no debe empezar con microservicios: la
complejidad operativa (registros distribuidos, deployments
coordinados, contratos versionados, tracing distribuido, mocks por
servicio) domina el diseño. El monolito modular con `IModule` te da los
beneficios del desacople (cada módulo es un assembly) sin pagar la
complejidad hasta que un módulo la necesite. Cuando un módulo pide
extraer a servicio (escala, seguridad, equipo dedicado), su
`Contracts` + `Outbox` ya están listos para eso. Ver
[`docs/adr/0001-monolito-modular.md`](adr/0001-monolito-modular.md).

## ¿Por qué Mediator y no MediatR?

MediatR requiere licencia comercial desde 2024 (versión 12+). La
plantilla usa `martinothamar/Mediator` (MIT) con source generator, que
soporta `sealed` parcial y elimina la reflexión. Los handlers genéricos
abiertos no se soportan bien en ningún bus de mensajes source-gen; por
eso el CRUD genérico vive en `ICrudService` y no como handler Mediator.
Ver [`docs/adr/0002-mediator.md`](adr/0002-mediator.md).

## ¿Por qué Guid v7 y no `long` autoincrement?

`long` autoincrement crea hotspots en escrituras concurrentes y obliga
a coordinar IDs entre instancias. `Guid.NewGuid()` (v4) genera
fragmentación de índices. `Guid.CreateVersion7()` (v7, time-ordered +
random) combina ambos: orden aproximado (útil para outbox y event
sourcing) y distribución. Ver
[`docs/adr/0004-guid-v7.md`](adr/0004-guid-v7.md).

## ¿Por qué la plantilla obliga a soft delete?

Porque el legacy borraba físicamente, dejaba los registros huérfanos y
rompía las FK lógicas. Toda entidad nueva implementa `ISoftDeletable`
y se elimina vía el `SoftDeleteInterceptor`. La prohibición del borrado
físico está como architecture test para que un PR no la rompa por
accidente. Se documenta en [`docs/guias/acceso-a-datos.md`](guias/acceso-a-datos.md)
y se enforza en [`tests/Project.ArchitectureTests`](../../tests/Project.ArchitectureTests).

## ¿Por qué OKLCH y no HSL?

HSL tiene un gamut desigual: el verde ocupa más espacio perceptual que
el azul. OKLCH (espacio de color perceptualmente uniforme) hace que
ajustar `lightness` se vea igual en todos los hues. Importante para
presets de marca consistentes.

## ¿Por qué `dotnet new module` no está listo todavía?

Las plantillas .NET requieren empaquetar los 4 csproj en
`.template.config/`. La estructura está en [`docs/07-agregar-un-modulo.md`](07-agregar-un-modulo.md)
y el script `scripts/new-module.mjs` cubre la parte frontend. Cuando
empaquetemos la plantilla .NET, ese comando aparecerá también.

## ¿Cómo agrego un nuevo preset de marca sin tocar el código central?

[`docs/guias/theming-y-marca.md`](guias/theming-y-marca.md) tiene el
paso a paso: agregas un bloque `[data-theme="mi-marca"]` en
`shared/theme/tokens.css` y registras el nombre en el array de
`presets` en `theme-customizer.tsx`. Nada más.

## ¿Cómo agrego una nueva sección de settings sin tocar código central?

Crea la clase que implementa `ISettingsSection`:

```csharp
public sealed class MySettings : ISettingsSection
{
    public static string SectionKey => "my-section";
    public string Foo { get; set; } = "default";
}
```

El `CascadingSettingsProvider` la descubre por assembly scan. Sin
edición de código central. El override se guarda vía
`ITenantSettingStore.SetAsync(scope, ..., sectionKey: "my-section", ...)`.

## ¿Por qué `Identity` está en `DataScope=Platform` y `Catalog` en `DataScope=Tenant`?

`Identity` (usuarios, roles, refresh tokens) vive en la BD de plataforma
porque un usuario puede pertenecer a varios tenants. `Catalog` (productos,
categorías) vive en la BD del tenant porque cada tenant tiene su propio
catálogo. La frontera [BD de plataforma / BD de tenant] sale del módulo
`Tenancy` (Fase 4B).

## ¿Cuándo uso `IMultiTenant` vs `IOrganizationScoped`?

- `IMultiTenant`: cuando el dato pertenece a un tenant (casi todo en
  `DataScope=Tenant`).
- `IOrganizationScoped`: cuando el dato pertenece a una unidad
  organizativa (empresa/marca/país/sucursal). Requiere `OrganizationModule`
  registrado. Si no, el filtro global se omite.

Un dato puede implementar ambas (ej: un producto que pertenece a una
marca dentro de un tenant).

## ¿Por qué el rate limit usa Redis?

Sin Redis, el rate limit es por instancia — el atacante puede
distribuir requests entre réplicas. Con Redis, el límite es global.

## ¿Cómo desactivo el login para integración?

`AllowAnonymous()` en el endpoint o `[AllowAnonymous]` a nivel de
controller. El fail-closed validator exige `[RequireOperation]` o
`[AllowAnonymous]` en cada endpoint; sin una de las dos, el arranque
falla.

## ¿Por qué JWT sin claim de operaciones?

Los permisos cambian seguido; meterlos en el token exige re-login en
cada cambio. La plantilla los cachea por `perm:{tid}:{userId}:{pv}` y
los invalida por `RolePermissionsChanged` (incrementa `pv`). El trade-off
es una llamada cache a `IPermissionService` por request — Redis local,
< 1 ms.

## ¿Cómo restarteo la contraseña del admin seed?

```bash
# Development
dotnet user-secrets set "Seed:AdminPassword" "NewStrongPassword"
dotnet run --project src/Host/Project.Api   # el seed corre al arrancar

# Production
Seed__AdminPassword=NewStrongPassword dotnet run --project src/Host/Project.MigrationService -- reseed
```

## ¿Dónde reporto una vulnerabilidad de seguridad?

[`SECURITY.md`](../SECURITY.md) — política de disclosure y contacto.