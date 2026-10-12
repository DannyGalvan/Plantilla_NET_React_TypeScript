# Ejecución, depuración y pruebas en local

**Para qué sirve este documento:** trabajar día a día con el backend y el frontend.

## Ejecutar

| Necesito | Comando |
|---|---|
| API + SPA juntos (SPA Proxy) | `dotnet run --project src/Host/Project.Api` |
| Solo API con referencia OpenAPI | `dotnet run --project src/Host/Project.Api --launch-profile api-only` y abrir `/scalar` |
| Recarga en caliente del backend | `dotnet watch --project src/Host/Project.Api` |
| Solo SPA | `cd src/project.client && pnpm dev` |
| Migrar y sembrar manualmente | `dotnet run --project src/Host/Project.MigrationService` |
| Correos de desarrollo | `docker compose up -d mailpit` y `Email__Host=localhost`, `Email__Port=1025`, `Email__EnableSsl=false` |

La SPA usa un proxy de Vite hacia `https://localhost:7180` (cambiable con `API_URL`). Para servirla por HTTP usa
`VITE_HTTPS=false`.

## Cambiar de motor de base de datos

```bash
docker compose --profile mysql up -d
dotnet user-secrets set "Database:Provider" "MySql" --project src/Host/Project.Api
dotnet user-secrets set "Database:ConnectionStrings:Default" "Server=localhost;Port=3306;Database=plantilla;User=root;Password=root" --project src/Host/Project.Api
```

## Depurar

- **Rider / Visual Studio:** abre `Project.slnx` y ejecuta el perfil `https` de `Project.Api`.
- **VS Code:** ejecuta `dotnet run` y adjunta el depurador al proceso `Project.Api`.
- Las respuestas 500 incluyen la propiedad `exception` solo en Development; en producción solo `traceId`.

## Pruebas

```bash
dotnet test tests/Project.UnitTests                 # rápidas, sin Docker
dotnet test tests/Project.ArchitectureTests         # reglas de dependencia
dotnet test tests/Project.Api.FunctionalTests       # API real + PostgreSQL en Docker
TEST_DB_PROVIDER=SqlServer dotnet test tests/Project.Api.FunctionalTests
TEST_DB_PROVIDER=MySql dotnet test tests/Project.Api.FunctionalTests
cd src/project.client && pnpm verify                # tipos, lint, formato, tokens y pruebas
```

```powershell
$env:TEST_DB_PROVIDER = 'SqlServer'; dotnet test tests/Project.Api.FunctionalTests
```

Más detalle en [guias/testing.md](guias/testing.md).
