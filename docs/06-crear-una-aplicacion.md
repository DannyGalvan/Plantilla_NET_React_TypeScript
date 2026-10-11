# 06 · Crear una aplicación

> Árbol de decisión + recetas por perfil. La guía paso a paso de "cómo
> extender" está en [`docs/07-agregar-un-modulo.md`](07-agregar-un-modulo.md).

## Árbol de decisión

```
¿Una empresa o varias?
├── Una sola                    -> perfil = simple
└── Varias
    ├── ¿Aislar infraestructura por cliente?
    │   ├── No, una BD basta     -> perfil = saas
    │   └── Sí, bases dedicadas  -> perfil = saas-dedicated
    └── ¿Necesitas jerarquía de unidades organizativas?
        ├── No                    -> Tenancy:Mode = SharedDatabase
        └── Sí                    -> Tenancy:Mode = SharedDatabase + OrganizationModule
```

## Recetas por perfil

### Perfil `simple` — App de inventario de un solo cliente

- Una sola empresa, una sola marca, una sola BD.
- `Tenancy:Mode = None`. `OrganizationModule` **no** se registra.
- Login: `1234` / `1234567`. Cambio forzado en el primer login.
- Idioma: `es` por defecto. `en` solo si lo activas.

```jsonc
// src/Host/Project.Api/appsettings.json
{
  "Tenancy": { "Mode": "None" },
  "Modules": { "Disabled": ["Organization"] }
}
```

### Perfil `erp` — Multi-empresa en una sola BD

- Varias empresas y marcas en una sola BD.
- `Tenancy:Mode = None`. `OrganizationModule` registrado.
- Cada empresa = `OrganizationUnitType = "Company"`. Cada marca = `"Brand"`.

```jsonc
{
  "Tenancy": { "Mode": "None" },
  "Modules": { "Disabled": [] }
}
```

`Program.cs`:

```csharp
builder.AddModules(
    new LegacyModule(),
    new TenancyModule(),
    new OrganizationModule(),
    new CatalogModule());
```

### Perfil `saas` — Multi-cliente con BD compartida

- Muchos clientes, una sola BD.
- `Tenancy:Mode = SharedDatabase`. `OrganizationModule` opcional (por
  cliente).
- Tenant se resuelve por host (`acme.app.local`, `globex.app.local`).

```jsonc
{
  "Tenancy": {
    "Mode": "SharedDatabase",
    "Resolvers": ["Host", "Subdomain"],
    "RootDomain": "app.local"
  }
}
```

### Perfil `saas-dedicated` — Multi-cliente con bases dedicadas

- Cada cliente con su propia BD (motor elegible por cliente: SqlServer,
  PostgreSQL, MySQL).
- `Tenancy:Mode = Hybrid`. Catálogo mixto shared ↔ Dedicated.
- Conexión vía `ISecretStore` (Key Vault, AWS Secrets Manager, env).

```jsonc
{
  "Tenancy": {
    "Mode": "Hybrid",
    "Resolvers": ["Host", "Subdomain", "Claim"]
  }
}
```

`move-tenant` (provision-tenant con `--to dedicated|shared`) migra la BD
del tenant en bloque con `ITenantDataMover` y un lock distribuido por
dialecto (`sp_getapplock` / `pg_advisory_lock` / `GET_LOCK`).

## Tres recetas completas

### (a) App de inventario (perfil `simple`)

1. Sigue [`docs/02-inicio-rapido.md`](02-inicio-rapido.md) camino C.
2. No instales `OrganizationModule`.
3. Crea el módulo `Inventory` siguiendo
   [`docs/07-agregar-un-modulo.md`](07-agregar-un-modulo.md). 1 módulo.

### (b) CRM (perfil `erp`)

1. Activa `OrganizationModule`.
2. Crea los módulos `Customers`, `Leads`, `Opportunities`, `Activities`.
3. La comunicación entre módulos va por `Contracts`. Ejemplo: el evento
   `LeadConverted` en `Leads` dispara la creación de un `Customer` en
   `Customers` vía outbox.
4. RBAC por organización: cada usuario solo ve los leads de su unidad.

### (c) SaaS (perfil `saas`)

1. `Tenancy:Mode = SharedDatabase`.
2. Crea el módulo `Tenancy` admin (en `src/Modules/Tenancy`).
3. `OrganizationModule` opcional. Activar solo para clientes que lo pidan.
4. La resolución de tenant es por host: `acme.app.local` → tenant `acme`,
   `globex.app.local` → tenant `globex`.
5. Cada tenant tiene su `BrandingSettings` y `LocalizationSettings`. El
   frontend carga `/platform/config` y aplica tokens vía `ThemeProvider`.

## Quitar lo que no se usa

- Para no usar Tenancy: borrar el módulo `Tenancy` y poner
  `Tenancy:Mode = None`.
- Para no usar Organization: poner `OrganizationModule` en
  `Modules:Disabled`.
- Para no usar Catalog (módulo de ejemplo): quitarlo de `Program.cs`.

## Cómo verificar el perfil

- `GET /platform/config` debe devolver `tenancyMode` igual al perfil.
- El log de arranque muestra `Tenancy: Resolvers in order = [...]`.
- En `DatabasePerTenant` o `Hybrid`, el log muestra las conexiones
  abiertas por tenant tras `dotnet run --project Project.MigrationService
  -- migrate`.