# 04 · Ejecución local

> Cómo correr backend y frontend juntos o por separado, hot reload,
> depuración y tests.

## Backend + frontend (recomendado)

Aspire orquesta los dos procesos y expone el dashboard de observabilidad:

```bash
dotnet run --project src/Host/Project.AppHost
```

El dashboard imprime la URL del API, el frontend y la traza OTLP.

## Backend nativo + frontend nativo

```bash
# Terminal 1
dotnet run --project src/Host/Project.Api

# Terminal 2
cd project.client
pnpm dev
```

Vite proxy `https://localhost:5173/api/*` al backend en
`https://localhost:5050`. La cookie de refresh es SameSite=Strict + Secure,
así que ambos procesos necesitan estar en HTTPS local. `dotnet dev-certs
https --trust` configura el certificado de desarrollo la primera vez.

## Hot reload

- **Backend**: `dotnet watch run --project src/Host/Project.Api` recompila y
  reinicia el proceso ante cada cambio. Los tests unitarios del propio
  proceso se ejecutan en `dotnet watch test`.
- **Frontend**: `pnpm dev` ya tiene HMR. Los componentes se recargan sin
  perder estado (mientras el componente sobreviva a `Fast Refresh`).

## Depuración

- **Rider / VS 2022 17.14+ / VS 2026**: abrir `src/Host/Project.Api/Project.Api.csproj`
  como proyecto de inicio. F5 arranca bajo el depurador. `launchSettings.json`
  configura `http://localhost:5050` por defecto.
- **VS Code**: abrir el repo, instalar la extensión C# Dev Kit, usar la
  configuración `.vscode/launch.json` (no incluida todavía — crear al
  clonar). El punto de entrada es `src/Host/Project.Api/Program.cs`.

## Cambiar de motor de BD en un minuto

```bash
docker compose --profile postgres up -d db    # o sqlserver / mysql
export Database__Provider=PostgreSql
export ConnectionStrings__Platform="Host=localhost;Database=plantilla;..."
dotnet run --project src/Host/Project.MigrationService -- migrate
dotnet run --project src/Host/Project.Api
```

## Correr tests

```bash
# Backend
dotnet test Project.sln -c Release
dotnet test tests/Project.ArchitectureTests -c Release
dotnet test tests/Project.Application.UnitTests -c Release

# Frontend
cd project.client
pnpm verify           # typecheck + lint + format + test
pnpm test             # vitest watch
pnpm test:e2e         # Playwright (cuando llegue Fase 8)
```

## Herramientas útiles en dev

- **Scalar** (OpenAPI UI): `http://localhost:5050/scalar` cuando el
  entorno sea `Development`.
- **Mailpit** (SMTP local): `http://localhost:8025`. Los mails enviados
  por `IEmailSender` caen ahí.
- **Aspire Dashboard** (cuando usas `Project.AppHost`): trazas OTLP,
  métricas, logs correlacionados por `traceId`.
- **página `/_design`** (cuando llegue Fase 7 slice 2): muestra todos los
  componentes con el tema actual. Solo disponible en `Development`.