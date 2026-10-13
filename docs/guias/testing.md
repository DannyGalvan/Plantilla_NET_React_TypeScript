# Pruebas

| Proyecto | Qué cubre | Requiere |
|---|---|---|
| `tests/Project.UnitTests` | Parser de filtros (parametrización, allowlist), Merge Patch, tenancy, dominio, política de contraseñas, validación de settings, seguridad de endpoints | Nada |
| `tests/Project.ArchitectureTests` | Reglas de dependencia entre capas y módulos, handlers sellados, entidades multi-tenant | Nada |
| `tests/Project.Api.FunctionalTests` | API real con `WebApplicationFactory` contra una base real en Docker: autenticación, refresh, bloqueo, permisos sin re-login, CRUD, reportes Dapper y SP, aislamiento por tenant (`SharedDatabase`), por unidad y bases dedicadas (`Hybrid`) | Docker |
| `src/project.client` (Vitest) | Cliente HTTP (refresh en cola única, ProblemDetails), guards, tema, tabla, registro de módulos, inicio de sesión | Nada |

## Ejecutar

```bash
dotnet test Project.slnx                                   # todo, PostgreSQL por defecto
TEST_DB_PROVIDER=SqlServer dotnet test tests/Project.Api.FunctionalTests
TEST_DB_PROVIDER=MySql dotnet test tests/Project.Api.FunctionalTests
cd src/project.client && pnpm test && pnpm test:coverage
```

CI ejecuta las funcionales en los tres motores en paralelo.

## Escribir pruebas funcionales

Usa `ApiFactory` (`NoneModeApi`, `SharedModeApi`, `HybridModeApi`) como `IClassFixture`. Cada fábrica crea su propia base,
aplica migraciones y semillas. `api.SignInAsync(user, password, host)` devuelve un cliente autenticado; el parámetro
`host` simula el dominio del tenant.
