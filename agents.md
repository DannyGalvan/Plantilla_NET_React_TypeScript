# Guía para agentes

Plantilla empresarial: monolito modular en .NET 10 y SPA modular en React 19 + TypeScript. Cubre desde una aplicación
de una sola empresa hasta un SaaS multi-tenant con bases dedicadas, solo por configuración.

## Estructura

```
src/BuildingBlocks/   Project.SharedKernel, Project.Application, Project.Infrastructure, Project.Web
src/Modules/<M>/      Project.Modules.<M>.{Domain,Application,Infrastructure,Contracts}  (Tenancy, Identity, Organization, Catalog)
src/Migrations/       Project.Migrations.{SqlServer,PostgreSql,MySql}  (carpeta por módulo)
src/Host/             Project.Api, Project.MigrationService, Project.Composition (AppModules), Project.ServiceDefaults
src/project.client/   SPA: src/app, src/shared, src/modules/<m>
tests/                Project.UnitTests, Project.ArchitectureTests, Project.Api.FunctionalTests
deploy/               docker, linux, iis
scripts/              setup, doctor, add-migration, new-module, check-docs
docs/                 guías, despliegue, adr
```

## Reglas del backend

- Dependencias: `Domain` solo SharedKernel; `Application` no usa EF Core ni ASP.NET Core; un módulo solo usa los
  `Contracts` de otro. Lo verifica `Project.ArchitectureTests`.
- Casos de uso: `ICommand`/`IQuery` y sus handlers de `Project.Application.Messaging`, handlers `sealed`, resultado
  `Result`/`Result<T>`. Validadores FluentValidation por convención.
- CRUD: `ICrudMapper` (Mapperly) + `QueryPolicy<T>` + validadores, y `MapCrud` en el endpoint.
- Permisos `{modulo}.{recurso}.{accion}` como constantes en `Contracts`; cada endpoint declara `RequirePermission`,
  `RequireAuthenticatedUser` o `AllowAnonymous` (fail-closed al arrancar).
- Entidades de negocio: `AuditableAggregateRoot`, `IMultiTenant` siempre, `IOrganizationScoped` si pertenecen a una
  unidad. Nunca leer `TenantId` u `OrganizationUnitId` del cuerpo de la petición.
- `IgnoreQueryFilters` solo en `Project.Infrastructure` y en el módulo Tenancy. SQL crudo siempre con `ScopedSql`.
- Async con `CancellationToken` propagado; `TimeProvider` en lugar de `DateTime.Now`.
- Cambios de modelo: `scripts/add-migration` genera la migración en los tres motores.
- Sin secretos en el repositorio: user-secrets o variables de entorno; ejemplos con `<placeholder>`.
- Dependencias nuevas con licencia MIT, Apache 2.0 o BSD; otras requieren ADR.

## Reglas del frontend

- Un módulo es una carpeta `src/modules/<m>` con `module.ts(x)` (manifiesto); el registro lo descubre solo.
- Tipos solo desde esquemas zod. Llamadas con `createCrudApi` y el `http-client` compartido.
- Colores solo con tokens semánticos (`pnpm tokens:check`). Textos con i18n (es y en).
- Rutas con `handle.permission`; acciones con `<Can>`. La API valida siempre.

## Comandos

```bash
dotnet build Project.slnx                         # advertencias como errores
dotnet test Project.slnx                          # requiere Docker
cd src/project.client && pnpm verify && pnpm build
node scripts/check-docs.mjs
node scripts/new-module.mjs <Modulo> <Entidad> [--org-scoped]
./scripts/add-migration.sh <Modulo> <Nombre>
graphify update .                                  # después de cambiar código
```

## Commits y PRs

- Conventional Commits, ramas desde `master`, sin commits directos a `master` ni reescritura de historia.
- Antes del PR: build, pruebas, `pnpm verify` y `check-docs` en verde.
- Documentación en español y sin emojis.

## Referencias

[docs/05-arquitectura.md](docs/05-arquitectura.md), [docs/07-agregar-un-modulo.md](docs/07-agregar-un-modulo.md),
[docs/adr](docs/adr).
