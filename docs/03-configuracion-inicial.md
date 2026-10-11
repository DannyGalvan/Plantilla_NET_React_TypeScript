# 03 · Configuración inicial

> Checklist de día 1 para un proyecto nuevo. Cada paso tiene un comando
> verificable al final.

## 1. Elegir perfil

| Perfil | `Tenancy:Mode` | `Organization` | Uso típico |
|---|---|---|---|
| `simple` | `None` | no | App de inventario de una sola empresa |
| `erp` | `None` | sí | ERP multi-empresa / multi-marca en una sola BD |
| `saas` | `SharedDatabase` | opcional | SaaS con muchos clientes en una BD |
| `saas-dedicated` | `Hybrid` | opcional | SaaS con bases dedicadas por cliente |

Edita `src/Host/Project.Api/appsettings.json` y reemplaza:

```jsonc
{
  "Tenancy": { "Mode": "None", "Resolvers": ["Host"] },
  "Modules": { "Disabled": [] }
}
```

Si elegiste `erp`, añade `OrganizationModule` en `Program.cs`:

```csharp
builder.AddModules(new LegacyModule(), new TenancyModule(),
                   new OrganizationModule(), new CatalogModule());
```

## 2. Elegir motor de BD

`Database:Provider` admite `SqlServer`, `PostgreSql`, `MySql`. La cadena de
conexión va en `Database:ConnectionStrings:Default` (convención) o en la
sección de cada módulo (`Identity`, `Catalog`, etc.).

```jsonc
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionStrings": {
      "Default": "Host=localhost;Database=plantilla;Username=plantilla;Password=plantilla"
    }
  }
}
```

## 3. Generar los secretos

| Secreto | Formato | Comando para generar |
|---|---|---|
| `Jwt:SigningKey` | ≥ 32 bytes UTF-8 | `openssl rand -base64 48` |
| `Seed:AdminPassword` | ≥ 12 chars con mixed case, digit, símbolo | (definido por el operador) |
| `Smtp:*` (opcional) | `host`, `port`, `user`, `password` | según proveedor |

En **Development** van en `dotnet user-secrets`. En **Staging/Production** van
en variables de entorno (la tabla de mapeo `:` → `__` está en [`docs/despliegue/`](despliegue/)).

```bash
dotnet user-secrets init --project src/Host/Project.Api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Seed:AdminPassword" "Choose-A-Strong-Password"
```

`AdminSeedHostedService` rechaza arrancar fuera de Development si
`Seed:AdminPassword` falta.

## 4. Configurar CORS y dominios

`CorsAllowedOrigins` en `appsettings.json`. En desarrollo basta con
`http://localhost:5173`. En producción, los dominios del frontend
(`https://app.example.com`).

```jsonc
{
  "AppSettings": {
    "CorsAllowedOrigins": [
      "http://localhost:5173",
      "https://app.example.com"
    ]
  }
}
```

## 5. Marca, idioma y moneda

Sin tocar `appsettings.json` (todo va por `BrandingSettings` y
`LocalizationSettings` por tenant). El primer tenant usa los defaults de
`new BrandingSettings()` y `new LocalizationSettings()`. Se editan luego en
`/platform/config` o vía el módulo Tenancy admin.

## 6. Redis (opcional pero recomendado para producción)

```jsonc
{
  "Cache": {
    "Redis": { "ConnectionString": "localhost:6379" }
  }
}
```

Sin Redis, HybridCache funciona solo en memoria (válido para un solo
nodo).

## 7. Observabilidad

```jsonc
{
  "Otel": { "Endpoint": "http://localhost:4317" }
}
```

Sin OTLP, los trazos van solo al console sink. En producción, configurar
`Otel:Endpoint` apunta al colector (Aspire, Jaeger, Tempo, Datadog…).

## 8. Correr migraciones y seed

```bash
dotnet run --project src/Host/Project.MigrationService -- migrate
```

Aplica primero los contextos `Platform` y luego los `Tenant`. En
`DatabasePerTenant`, itera sobre el catálogo.

## 9. Primer login y bootstrap

```bash
dotnet run --project src/Host/Project.Api
```

Abre `http://localhost:5050/health/live` (200 OK) y
`http://localhost:5050/platform/config` (200 OK con el branding por
defecto). Luego `cd project.client && pnpm dev` y entra con `1234` /
`1234567`. Cambia la contraseña en el primer login (forzado por
`MustChangePassword`).

## 10. Renombrar el proyecto

```bash
./scripts/new-app.sh MyCompany.Erp
# o en Windows:
scripts/new-app.ps1 MyCompany.Erp
```

Renombra namespaces, slugs y el `UserSecretsId`. Imprime la única línea
externa que hay que cambiar en `Program.cs`.