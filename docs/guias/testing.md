# Testing

> Estrategia de pruebas para backend y frontend.

## Backend

| Tipo | Herramienta | Qué cubre |
|---|---|---|
| Unit | xUnit + coverlet | handlers, validators, projections, parsers |
| Architecture | NetArchTest | reglas de dependencia, IMultiTenant, no-EF-en-módulos |
| Integration | Testcontainers (MsSql/PostgreSQL/MySQL/Redis) + Respawn | CRUD genérico, MapCrud, EF interceptors |
| Isolation | xUnit + Testcontainers | un test reflexivo sobre todos los endpoints GET de listado: 2 tenants, 2 marcas dentro de uno. Verifica que ningún endpoint devuelve, cuenta, actualiza o borra filas de otro tenant. |
| Load | k6 | smoke de 200 VUs / 2 min sobre los endpoints críticos |

## Frontend

| Tipo | Herramienta | Qué cubre |
|---|---|---|
| Unit | Vitest + Testing Library | hooks, stores, parsers, schemas |
| Component | Vitest + jsdom | UI primitives (Button, Card, Dialog, Tooltip, Form) |
| Mock HTTP | MSW | interceptores de axios, refresh-on-401, parseProblem |
| E2E | Playwright | login + CRUD + theme switch + permission denegado + cambio de contexto |

## Matriz CI

`TEST_DB_PROVIDERS=SqlServer,PostgreSQL,MySQL` ×
`TEST_TENANCY_MODES=None,SharedDatabase,DatabasePerTenant,Hybrid`.

- **PRs**: SqlServer con `SharedDatabase` + todos los modos con
  PostgreSQL.
- **Master / nightly**: matriz completa.

Jobs del CI:
- `backend-test` — `dotnet test` con la matriz.
- `architecture-test` — `dotnet test tests/Project.ArchitectureTests`.
- `isolation` — `dotnet test tests/Project.IsolationTests` (bloqueante).
- `has-pending-model-changes` — `dotnet ef migrations has-pending-model-changes`.
- `docs` — markdownlint + lychee + check-no-emoji.
- `publish-smoke` — IIS + Linux con `MigrationService` + smoke `/health/ready`.
- `quickstart-matrix` — Ubuntu, Windows, macOS con `dotnet tool restore`,
  `dotnet build`, `pnpm install`, `pnpm build`.

## Test reflexivo de aislamiento

`Project.IsolationTests` recorre todos los endpoints GET de listado
registrados. Para cada uno crea 2 tenants + 2 marcas dentro del primero,
inserta datos, y verifica:

- `GET /<resource>` del tenant A no devuelve filas del tenant B.
- `GET /<resource>/{id}` de un id del tenant B devuelve 404.
- `GET /<resource>` del tenant A con filtro `tenantId=B` devuelve 0 filas.
- Cache: la misma key lógica en tenant A y tenant B devuelve valores
  distintos (sin contaminación cruzada).
- Outbox: un mensaje emitido en tenant A se procesa con el contexto
  de tenant A.
- `ForEachTenantAsync`: el job itera sin mezclar contextos bajo
  concurrencia.

Agregar un módulo nuevo cubre automáticamente el test reflexivo: sus
endpoints GET de listado entran al barrido sin tocar el test.

## Coverage

- Coverlet threshold 70% en `Project.Application` y `Project.SharedKernel`.
- `@vitest/coverage-v8` threshold 70% en `project.client/src/shared/`.
- Coverage visible en el PR (codecov o report upload).

## Cómo correr todo localmente

```bash
# Backend
dotnet test Project.sln -c Release

# Architecture
dotnet test tests/Project.ArchitectureTests -c Release

# Frontend
cd project.client
pnpm verify
pnpm test:e2e   # cuando Playwright esté listo (Fase 8)
```

## TDD cuando agregues un módulo

1. Crea los tests del handler (fakes de `IRepository`).
2. Crea el handler y el DTO.
3. Crea el MapCrud + integración con Testcontainers (motor real).
4. Crea el test de aislamiento para los nuevos endpoints.
5. Smoke e2e en Playwright.