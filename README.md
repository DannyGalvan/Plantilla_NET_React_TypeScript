# Plantilla_NET_React_TypeScript

Base empresarial modular en .NET 10 LTS y React 19 + TypeScript para construir desde una app de inventario de una sola empresa hasta un SaaS multi-tenant con bases dedicadas, todo por configuración y sin código a medida.

## Características

- **Arquitectura modular por BuildingBlocks**: SharedKernel, Application, Infrastructure, Web. Cada módulo expone 4 proyectos (Domain, Application, Infrastructure, Contracts).
- **CQRS + Mediator source-gen**: pipelines componibles (Logging → Validation → Authorization → Caching → Transaction).
- **Multi-proveedor**: SqlServer, PostgreSQL, MySQL (Oracle fallback mientras Pomelo publica EF 10). Estrategia `IDatabaseProvider`.
- **Tenancy configurable**: `Tenancy:Mode` admite `None`, `SharedDatabase`, `DatabasePerTenant`, `Hybrid`. Catalog de tenants con HybridCache. Organización opcional con `AccessibleUnitIds`.
- **Settings en cascada**: System → Tenant → OrgUnit → User. Cada sección implementa `ISettingsSection`; sin código central al agregar.
- **Theming con tokens CSS en OKLCH**: `ThemeProvider` aplica modo, preset, density, fontScale, primaryOverride y radius. `ThemeCustomizer` Sheet accesible. Single build sirve múltiples marcas.
- **Multi-idioma** (es + en) con `react-i18next` y `IStringLocalizer`.
- **Seguridad por defecto**: JWT liviano (`sub`/`tid`/`pv`), BCrypt (work 12), HybridCache para permisos (`perm:{tid}:{userId}:{pv}`), refresh cookie HttpOnly + CSRF double-submit, rate-limit distribuido con Redis.
- **Accesibilidad**: Radix + jsx-a11y, contraste AA, contraste AA verificado en CI.

## Stack

| Capa | Tecnología |
|---|---|
| Backend | .NET 10 LTS, EF Core 10, Mediator (martinothamar), Mapster + Mapperly, FluentValidation |
| Backend infra | Serilog, OpenTelemetry, HybridCache (Redis opcional), Health Checks, Aspire ServiceDefaults |
| Frontend | React 19, TypeScript 5, Vite, Zustand, TanStack Query, axios, react-router |
| Frontend UI | shadcn/ui + Radix + Tailwind v4 con tokens CSS en OKLCH |
| Test | xUnit + NetArchTest + Testcontainers + Respawn (Fase 5); Vitest + MSW + Playwright (Fase 8) |
| DB | SQL Server 2019+, PostgreSQL 15+, MySQL 8+ |
| Auth | JWT HS256 + BCrypt + refresh cookies + CSRF double-submit |

## Prerrequisitos

Lee [`docs/01-prerequisitos.md`](docs/01-prerequisitos.md) para la lista completa y los pasos de instalación por SO.

## Inicio rápido

```bash
# Backend
git clone https://github.com/DannyGalvan/Plantilla_NET_React_TypeScript
cd Plantilla_NET_React_TypeScript
dotnet build Project.sln -c Release -warnaserror

# Frontend
cd project.client && pnpm install && cd ..
docker compose --profile postgres up -d
dotnet run --project src/Host/Project.MigrationService   # migrate
dotnet run --project src/Host/Project.Api                 # API en :5050
cd project.client && pnpm dev                            # Vite en :5173
```

Usuario seed: `1234` · contraseña: `1234567` (configurable vía `Seed__AdminPassword`).

## Estructura del repositorio

```
src/
  BuildingBlocks/   # Project.SharedKernel, Project.Application, Project.Infrastructure, Project.Web
  Host/             # Project.Api, Project.ServiceDefaults, Project.MigrationService
  Modules/          # Identity, Tenancy, Organization, Catalog, + los que agregues
tests/
  Project.ArchitectureTests   # reglas de dependencia, no-EF-on-modules, IMultiTenant invariant
  Project.Application.UnitTests
  Project.Server.Tests        # legacy
docs/                          # architecture, adding-a-module, data-access, security, adr/
project.client/                # frontend (shared/theme, shared/ui, shared/api, shared/auth, modules/)
plans/                           # plan maestro + sub-planes
scripts/                        # new-module.mjs, add-migration.sh, setup, doctor
```

## Documentación

- [`docs/architecture.md`](docs/architecture.md) — capas, reglas, multi-tenancy, settings, theming.
- [`docs/adding-a-module.md`](docs/adding-a-module.md) — backend (4 proyectos) y frontend (manifest-driven).
- [`docs/data-access.md`](docs/data-access.md) — EF/Dapper/SP/bulk, multi-provider, IgnoreQueryFilters prohibido fuera de Infrastructure.
- [`docs/security.md`](docs/security.md) — JWT, refresh, lockout, BCrypt, fail-closed, ProblemDetails, CSP.
- [`docs/troubleshooting.md`](docs/troubleshooting.md) — errores frecuentes por SO y por motor.
- [`docs/faq.md`](docs/faq.md)
- [`docs/glosario.md`](docs/glosario.md)
- [`docs/adr/`](docs/adr/) — decisiones arquitectónicas (monolito modular, Mediator, Minimal APIs, Guid v7, shadcn/ui OKLCH, tenancy).
- [`docs/despliegue/`](docs/despliegue/) — Docker, IIS, Linux systemd + nginx, Kubernetes, App Service.
- [`docs/guias/`](docs/guias/) — acceso-a-datos, multitenancy, organización, seguridad, permisos-y-navegacion, theming-y-marca, i18n, testing, escalabilidad, observabilidad, background-jobs-y-eventos.

## Roadmap

Consulta [`plans/2026-10-10_refactor-plantilla-modular-empresarial.md`](plans/2026-10-10_refactor-plantilla-modular-empresarial.md) para el plan maestro y los criterios de salida por fase.

## Contribuir

[`agents.md`](agents.md) contiene las convenciones obligatorias: backend async/CT, frontend OKLCH tokens, manifest-driven modules, prohibido borrado físico (todo implementa `ISoftDeletable`), sin secretos en `localStorage`. Antes de abrir un PR, lee los ADRs.

## Licencia

MIT — Copyright (c) 2026 Danny Galvan. Ver [`LICENSE`](LICENSE).