# 08 · Tecnologías

> Versiones fijadas y licencias por capa. Todas las dependencias se
> centralizan en `Directory.Packages.props` (CPM). Las reemplazos siguen
> `dotnet list package --outdated` + PR con la bump + actualización de
> tests.

## Backend

| Tecnología | Versión | Uso | Licencia |
|---|---|---|---|
| .NET SDK | 10.0.x (`global.json`) | runtime + tooling | MIT |
| ASP.NET Core | 10.0.x | web framework | MIT |
| EF Core | 10.0.x | ORM + migrations | MIT |
| EF Core.SqlServer | 10.0.x | provider SQL Server | MIT |
| Npgsql.EFCore.PostgreSQL | 10.0.x | provider PostgreSQL | MIT |
| MySql.EntityCore.EFCore | 10.0.x | provider MySQL (Oracle, fallback Pomelo) | MIT |
| Mediator.Abstractions | 3.0.x | CQRS pipeline | MIT |
| Mediator.SourceGenerator | 3.0.x | handlers sin reflection | MIT |
| FluentValidation | 12.0.x | input validation | Apache-2.0 |
| Mapperly | 3.6.x | source-gen mapping (preferido) | MIT |
| Mapster | 7.4.x | mapping legacy (a migrar) | MIT |
| BCrypt.Net-Next | 4.0.x | password hashing | MIT |
| Serilog + AspNetCore | 4.3.x / 10.0.x | structured logging | Apache-2.0 |
| OpenTelemetry | 1.16.x | tracing + metrics + OTLP | Apache-2.0 |
| Microsoft.Extensions.Caching.Hybrid | 10.0.x | L1+L2 cache (Redis opcional) | MIT |
| Microsoft.AspNetCore.OpenApi | 10.0.x | OpenAPI nativo | MIT |
| Scalar.AspNetCore | 2.1.x | UI para OpenAPI | MIT |
| Asp.Versioning.Http | 8.1.x | versionado de endpoints | MIT |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.x | bearer auth | MIT |
| Microsoft.IdentityModel.Tokens | 8.0.x | JWT HMAC | MIT |
| System.IdentityModel.Tokens.Jwt | 8.0.x | JWT encode/decode | MIT |
| Swashbuckle.AspNetCore | 9.0.x | Swashbuckle (legacy path) | MIT |

> Solo MIT y Apache-2.0. Las dependencias copyleft (GPL/AGPL) o
> comerciales requieren un ADR antes de agregarse.

## Frontend

| Tecnología | Versión | Uso | Licencia |
|---|---|---|---|
| Node.js | 24.15.x (`.nvmrc`) | runtime + tooling | MIT |
| pnpm | 9.15.x (`packageManager`) | package manager | MIT |
| React | 19.x | UI framework | MIT |
| TypeScript | 5.8.x | type system | Apache-2.0 |
| Vite | 6.3.x | bundler + dev server | MIT |
| Zustand | 5.x | state | MIT |
| TanStack Query | 5.x | server state | MIT |
| axios | 1.x | HTTP | MIT |
| react-router | 7.x | routing | MIT |
| react-i18next | (Fase 7) | i18n | MIT |
| zod | 4.x | runtime + compile-time validation | MIT |
| shadcn/ui | latest | components | MIT |
| Radix UI | latest | accessibility primitives | MIT |
| Tailwind CSS | 4.x | utility CSS + OKLCH | MIT |
| Vitest | 5.x | unit tests | MIT |
| @testing-library/react | 16.x | component tests | MIT |
| MSW | (Fase 8) | HTTP mocks | MIT |
| Playwright | (Fase 8) | e2e | MIT |
| ESLint | 9.x | linting | MIT |
| Prettier | 3.x | formatting | MIT |

## Tests

| Tecnología | Versión | Uso | Licencia |
|---|---|---|---|
| xUnit | 2.9.x | unit/integration tests | MIT |
| coverlet.collector | 6.0.x | coverage | MIT |
| Testcontainers (.NET) | 3.x | SqlServer / PostgreSQL / MySQL / Redis containers | MIT |
| Respawn | 6.x | DB cleanup between tests | MIT |
| NetArchTest.eNhancedEdition | 1.4.x | architecture rules | Apache-2.0 |

## DevOps

| Tecnología | Uso |
|---|---|
| Docker + Compose v2 | dev stack |
| GitHub Actions | CI (matrix provider × tenancy) |
| Aspire | local orchestration |
| OpenTelemetry Collector / Jaeger / Datadog | prod tracing |
| Mailpit | dev SMTP |