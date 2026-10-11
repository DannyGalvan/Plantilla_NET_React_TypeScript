# Architecture

> Resumen ejecutivo del monolito modular con Clean Architecture por módulo.

## Capas

```
+----------------------------------------------------+
|  Project.Api (M)   -  Host delgado, AddModules()    |
|  Project.ServiceDefaults (M) - Serilog/OTel/Health |
|  Project.MigrationService (worker) - Migraciones   |
+----------------------------------------------------+
|  BuildingBlocks                                      |
|  Project.SharedKernel  (Domain puro)                |
|  Project.Application   (Mediator + behaviours)     |
|  Project.Infrastructure (EF/Dapper/Tenancy/Outbox) |
|  Project.Web           (IModule + ResultExtensions) |
+----------------------------------------------------+
|  Modules (Identity, Tenancy, Organization, Catalog, |
|  … nuevos: dotnet new template genera 4 proyectos) |
+----------------------------------------------------+
```

## Reglas de dependencia

- `Domain` solo depende de `SharedKernel`.
- `Application` depende de `Domain`, `Application`, y los `Contracts` de
  otros módulos.
- `Infrastructure` depende de `Application`, `Project.Infrastructure`,
  `Project.Web`.
- Un módulo nunca referencia `Domain`/`Application`/`Infrastructure` de
  otro módulo; se comunican por `Contracts`.

Los `ArchitectureTests` en `tests/Project.ArchitectureTests` validan
estas reglas en CI.

## Multi-tenancy

`ITenantContext` vive en `Project.Infrastructure.Tenancy`. El módulo
`Tenancy` mantiene el catálogo (Tenant, TenantDomain, TenantFeature,
SettingValue). La resolución es por cadena de `ITenantResolver`
configurados en `Tenancy:Resolvers`.

- `Tenancy:Mode = None` → un solo tenant `default` sin I/O.
- `Tenancy:Mode = SharedDatabase` → una sola BD, filtro por `TenantId`.
- `Tenancy:Mode = DatabasePerTenant` → una BD por tenant, conexión vía
  `ISecretStore`.
- `Tenancy:Mode = Hybrid` → mixto, transición con `move-tenant`.

## Settings en cascada

`SettingsProvider` JSON-mergea en orden:
`new T()` → appsettings → System → Tenant → OrganizationUnit (root →
active) → User. La sección más específica gana por propiedad. Los
`ISettingsSection` se descubren por assembly scan; agregar una sección no
edita código central.

## Frontend

`src/shared/theme/tokens.css` es la única fuente de color (OKLCH).
`ThemeProvider` aplica modo (light/dark/system), preset, density,
fontScale, primaryOverride y radius sobre `<html>`. Los módulos exponen un
`ModuleManifest` con `id`, `routes`, `nav`, `features`, `i18n` y se
descubren con `import.meta.glob` desde `app/module-registry.ts`.