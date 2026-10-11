# Agentes - Plantilla NET 10 + React 19 + TypeScript

## Descripción General

Esta plantilla sirve como marco de trabajo empresarial con monolito modular
en .NET 10 y frontend modular en React 19 + TypeScript. Cubre desde una
app de inventario de una sola empresa hasta un SaaS multi-tenant con
bases dedicadas, todo por configuración.

## Estructura del Proyecto

```
Plantilla_NET_React_TypeScript/
├── Project.Server/              # Legacy single-project (a eliminar en Fase 4)
├── Project.Server.Tests/        # Tests del legacy
├── project.client/              # Frontend legacy + shared/theme, shared/ui, shared/auth…
├── src/
│   ├── BuildingBlocks/          # Project.SharedKernel, Project.Application,
│   │                            # Project.Infrastructure, Project.Web
│   ├── Host/                    # Project.Api (host delgado),
│   │                            # Project.ServiceDefaults, Project.MigrationService
│   ├── Modules/                 # Identity, Tenancy, Organization, Catalog,
│   │                            # más los que agregues con new-module
├── tests/                       # Project.ArchitectureTests + FunctionalTests (Fase 5)
├── scripts/                     # new-module.mjs, add-migration.sh
├── docs/                        # architecture, security, adding-a-module…
│   └── adr/                     # ADRs numeradas
├── plans/                       # Plan maestro + sub-planes
└── graphify-out/                # Knowledge graph (AST + del código)
```

## Convenciones del Backend (plan §3.3)

- **Permisos**: `{module}.{resource}.{action}` en minúsculas (p. ej.
  `inventory.products.read`). Se declaran como constantes en el
  `Contracts/Permissions.cs` del módulo. Nunca derivados del nombre del
  controller.
- **Async**: todo método async recibe y propaga `CancellationToken ct`.
  Prohibido `.Result` / `.Wait()`.
- **Sin estáticos con estado**. Sin service locator.
- **Logging**: structured logging con `LoggerMessage` source-generated en
  hot paths.
- **Filtros de query**: combinar `IMultiTenant`, `IOrganizationScoped`,
  `ISoftDeletable` en un único `HasQueryFilter`. Prohibido
  `IgnoreQueryFilters()` fuera de `Project.Infrastructure`.
- **Borrado físico PROHIBIDO**: implementar `ISoftDeletable`. El
  `SoftDeleteInterceptor` convierte `EntityState.Deleted` en `Modified`
  con `IsDeleted=true`.
- **Sin `DateTime.Now`**: usar `TimeProvider` (inyectado).
- **Entities**: implementar `IMultiTenant` cuando `DataScope=Tenant`.
  Implementar `IOrganizationScoped` cuando el dato pertenece a una
  unidad organizativa.
- **Multi-tenancy**: leer siempre de `ITenantContext` u
  `IOrganizationContext`. Nunca del request body.
- **Sin secretos** en el repo: `dotnet user-secrets` o variables de
  entorno. Plantillas con `<TU_VALOR>`.

## Convenciones del Frontend

- **Tokens CSS** en OKLCH como única fuente de color
  (`src/shared/theme/tokens.css`).
- **Módulos** con `ModuleManifest` (id, routes, nav, features, i18n). El
  registry (`src/app/module-registry.ts`) los descubre con
  `import.meta.glob`. Agregar un módulo = carpeta + manifest; nada más.
- **Borrado físico backend** → no exponer `Delete` directo en UI;
  siempre detrás de un confirm dialog.
- **CSP `<meta>`** en `index.html`. `target="_blank"` siempre con
  `rel="noopener noreferrer"`. Sin secretos en `localStorage`.

## Comandos Habituales

- `dotnet build Project.sln --configuration Release -warnaserror`
- `dotnet test Project.sln --no-build --configuration Release`
- `cd project.client && pnpm verify`  (typecheck + lint + format + vitest)
- `graphify update .`
- `node scripts/new-module.mjs <name>` (frontend)
- `./scripts/add-migration.sh <Module> <Name>` (backend EF)

## Reglas Para Commits Y PRs

- Conventional Commits (`feat:`, `fix:`, `chore:`, `refactor:` …).
- Commits pequeños, una fase por rama y por PR.
- Una rama `refactor/fase-N-<slug>` por fase.
- PR siempre con resumen, la checklist de la fase y la evidencia de
  verificación.
- No commit directo a `master`, no reescribir historia.
- Marcar `[x]` en el archivo de plan al completar cada paso. Si un paso se
  adapta, añadir una nota breve "Nota de implementación".

## Stack Tecnológico

### Backend
- .NET 10 LTS, C# 14, ASP.NET Core, EF Core 10, Mediator (martinothamar).
- Multi-provider: SqlServer (Microsoft.Data.SqlClient), PostgreSQL
  (Npgsql), MySQL (Oracle MySql.EntityFrameworkCore).
- OpenTelemetry + Serilog + HybridCache (Redis opcional).

### Frontend
- React 19, TypeScript, Vite, zustand, axios, TanStack Query, react-router.
- shadcn/ui + Radix + Tailwind v4 + tokens CSS en OKLCH.
- Playwright (e2e) + MSW (unit) + vitest.

## ADRs

Lee `docs/adr/0001-monolito-modular.md` y siguientes para entender las
elecciones arquitectónicas. Cada ADR tiene estado, fecha y consecuencias.

## Pendientes Conocidos

- El legacy `Project.Server` se borra cuando Identity / Catalog /
  Tenancy / Organization se portan a los módulos nuevos.
- Migrations por proveedor (`Project.Migrations.{SqlServer,PostgreSql,MySql}`)
  se crean con el script `add-migration.sh` cuando llegue el DbContext
  del primer módulo.