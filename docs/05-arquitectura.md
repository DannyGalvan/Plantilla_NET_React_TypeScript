# 05 · Arquitectura

> Cómo está estructurada la plantilla, qué patrones usa y por qué. La
> descripción detallada de cada decisión vive en [`docs/adr/`](adr/).

## Diagrama de capas

```
+--------------------------------------------------------+
|  Host                                                    |
|  Project.Api (worker)   Program.cs delgado, AddModules() |
|  Project.ServiceDefaults       Serilog / OTel / Health  |
|  Project.MigrationService      Worker de migraciones      |
|  Project.AppHost               Aspire AppHost             |
+--------------------------------------------------------+
|  BuildingBlocks                                           |
|  Project.SharedKernel   Domain primitives (Entity,       |
|                          AggregateRoot, Result/Error,     |
|                          IMultiTenant, ISoftDeletable)    |
|  Project.Application    Mediator contracts, behaviours,   |
|                          Pagination, IDatabaseProvider     |
|  Project.Infrastructure EF/Dapper, IDbConnectionFactory, |
|                          Tenancy, Outbox                   |
|  Project.Web            IModule, ResultExtensions,         |
|                          GlobalExceptionHandler, MapCrud   |
+--------------------------------------------------------+
|  Modules                                                  |
|  Identity (Platform)             Tenancy (Platform)         |
|  Organization (Tenant, opcional) Catalog (Tenant)         |
|  + los que agregues con \`dotnet new module\` o           |
|    \`scripts/new-module.mjs\`                              |
+--------------------------------------------------------+
```

## Patrones

| Patrón | Dónde | Por qué |
|---|---|---|
| CQRS | `ICommand` / `IQuery` en `Project.Application/Mediator` | Separación lectura/escritura, handlers `sealed` |
| Repository + UoW | `IRepository<T,TId>` + `IUnitOfWork` en `Project.Application/Abstractions` | Sustitución de EF sin tocar dominio |
| Specification | `ISpecification<T>` + `SpecificationEvaluator` | Filtros y orden componibles sobre IQueryable |
| Strategy (provider) | `IDatabaseProvider` | Motor intercambiable (SqlServer/PostgreSql/MySql) |
| Strategy (filtros) | `IFilterOperator` | Operadores extensibles sin tocar el parser |
| Decorator / pipeline | `IPipelineBehavior` (Logging, Validation, Authorization, Caching, Transaction) | Cross-cutting en Mediator |
| Outbox | `OutboxMessage` + `OutboxProcessor` | Eventos sincrónicos con handlers asíncronos |
| Result | `Result<T>` + `Error` | Errores esperados como valores, no excepciones |
| Options | `IOptions<T>` + `ValidateDataAnnotations().ValidateOnStart()` | Configuración tipada y validada al arrancar |

## Reglas de dependencia

Las reglas las enforza `Project.ArchitectureTests`:

- `Domain` solo depende de `SharedKernel`.
- `Application` depende de `Domain` y `Project.Application`; nunca de EF.
- `Infrastructure` depende de `Application`, `Project.Infrastructure`,
  `Project.Web`.
- Un módulo nunca referencia `Domain`/`Application`/`Infrastructure` de
  otro módulo. Se comunican por `Contracts`.
- Los handlers son `sealed`.
- Los módulos de negocio no referencian `Microsoft.EntityFrameworkCore`
  directamente (todo pasa por `Project.Infrastructure`).

## Multi-tenancy (matriz §3.4)

```
Tenancy:Mode = None                 -> Tenant implícito "default"
Tenancy:Mode = SharedDatabase       -> Filtro global por TenantId
Tenancy:Mode = DatabasePerTenant    -> Conexión por tenant vía ISecretStore
Tenancy:Mode = Hybrid              -> Catálogo decide shared/dedicated
```

`ModuleDbContext` aplica los filtros como un único `HasQueryFilter` por
entidad. `TenantInterceptor` valida el `TenantId` en cada `Insert`/`Modify`.

## Settings en cascada

```
new T()                          (defaults del record)
  ↓
appsettings.json (Settings:{Section})
  ↓
System       (Tabla SettingValue scope=System)
  ↓
Tenant       (scope=Tenant)
  ↓
OrganizationUnit (root → active)
  ↓
User         (scope=User)
```

Cada capa es JSON-merge: el valor más específico gana por propiedad.
Las secciones (`ISettingsSection`) se descubren por assembly scan; agregar
una sección no edita código central.

## Theming

Tokens CSS en OKLCH como única fuente de color. `ThemeProvider` aplica:

- `mode`: `light` / `dark` / `system` (sigue `prefers-color-scheme`).
- `preset`: `default` / `ocean` / `forest` / `sunset` / `corporate`.
- `density`: `compact` / `comfortable` (cambia `--spacing`).
- `fontScale`: `sm` / `md` / `lg` (cambia `font-size` raíz).
- `primaryOverride`: color OKLCH libre, validado en servidor con contraste AA.
- `radius`: valor libre, cambia `--radius`.

Persiste en `localStorage:brand:theme`. Se aplica via atributos
`data-theme`, `data-density` y `.dark` en `<html>`.

## Frontend

- React 19, TypeScript 5, Vite 6, Zustand 5, TanStack Query 5, axios.
- shadcn/ui + Radix + Tailwind v4 con tokens CSS en OKLCH.
- Módulos via `ModuleManifest` (id, routes, nav, features, i18n). El
  `app/module-registry.ts` los descubre con `import.meta.glob`.
- `HttpClient` con refresh-on-401 en cola (single-flight).
- Tokens, errores y secretos **nunca** en `localStorage`. Solo el
  access token vive en memoria dentro del módulo `shared/auth`.

## Diagramas

Los diagramas Mermaid del flujo de un request y de la jerarquía de
módulos viven en `docs/diagrams/` (cuando llegue Fase 10 slice 2) y se
mantienen sincronizados con `graphify-out/graph.html`.