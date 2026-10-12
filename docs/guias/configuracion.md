# Referencia de configuración

Valores por defecto en `src/Host/Project.Api/appsettings.json`. En variables de entorno, `:` se escribe `__`
(`Database:ConnectionStrings:Default` es `Database__ConnectionStrings__Default`). Los secretos van en user-secrets
(desarrollo) o variables de entorno / almacén de secretos (producción).

## Database

| Clave | Tipo | Defecto | Obligatoria en producción | Descripción |
|---|---|---|---|---|
| `Provider` | texto | `PostgreSql` | Sí | `PostgreSql`, `SqlServer` o `MySql` |
| `ConnectionStrings:Default` | texto | vacío | Sí (secreto) | Base de plataforma y de tenants compartidos |
| `ConnectionStrings:Platform` | texto | | No | Base separada para los módulos de plataforma |
| `ConnectionStrings:<Modulo>` | texto | | No | Base propia de un módulo (por ejemplo `Catalog`) |
| `CommandTimeoutSeconds` | entero | 30 | No | Tiempo máximo por comando |
| `MaxRetryCount` | entero | 5 | No | Reintentos ante errores transitorios |
| `EnableSensitiveDataLogging` | bool | false | No | Solo surte efecto en Development |
| `MigrateOnStartup` | bool | false (true en Development) | Prohibido en Production | Migra y siembra al iniciar la API |

## Tenancy

| Clave | Defecto | Descripción |
|---|---|---|
| `Mode` | `None` | `None`, `SharedDatabase`, `DatabasePerTenant`, `Hybrid` |
| `Resolvers` | `["Host","Subdomain","Claim"]` | Estrategias en orden; `Header` (`X-Tenant`) solo para servicios internos |
| `RootDomain` | | Dominio base para resolver `acme.<RootDomain>` |
| `HeaderName` | `X-Tenant` | Cabecera del resolvedor `Header` |
| `DefaultTenantName` | `Default` | Nombre del tenant implícito |
| `DefaultTenantDomains` | `["localhost","127.0.0.1"]` | Dominios del tenant implícito |

## Jwt

| Clave | Defecto | Descripción |
|---|---|---|
| `SigningKey` | vacío | Obligatoria, 32 caracteres o más (secreto) |
| `Issuer` / `Audience` | `project-api` / `project-client` | Emisor y audiencia |
| `AccessTokenMinutes` | 15 | Vida del token de acceso |
| `RefreshTokenDays` | 7 | Vida del refresh token |

## Otras secciones

| Sección | Claves principales |
|---|---|
| `Identity` | `PublicUrl` (enlaces de restablecimiento), `PasswordResetMinutes` |
| `Seed` | `AdminUserName`, `AdminEmail`, `AdminPassword` (obligatoria fuera de Development), `SampleData` |
| `Cache:Redis` | `ConnectionString` (opcional; recomendado con varias instancias) |
| `RateLimiting` | `Enabled`, `GlobalPermitPerMinute`, `AuthPermitPer15Minutes` |
| `Pagination` | `MaxPageSize` |
| `Hosting` | `ServeSpa`, `EnableApiExplorer`, `UseHttpsRedirection`, `KnownProxies`, `KnownNetworks` |
| `Cors` | `AllowedOrigins` (solo si la SPA vive en otro dominio) |
| `Email` | `Host`, `Port`, `EnableSsl`, `UserName`, `Password`, `From` |
| `Outbox` | `Enabled`, `PollingIntervalSeconds`, `BatchSize`, `MaxAttempts` |
| `Features` | `<nombre>: true/false` valores por defecto de los feature flags |
| `Settings:<seccion>` | Valores por defecto de las secciones de settings (`branding`, `localization`, `tax`, `security`) |
| `Secrets` | `<referencia>`: cadenas de conexión de tenants dedicados (o reemplaza `ISecretStore`) |
| `Modules` | `Disabled`: módulos que no se cargan |
| `Migrations` | `MaxParallelTenants` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Endpoint OTLP para trazas, métricas y logs |
