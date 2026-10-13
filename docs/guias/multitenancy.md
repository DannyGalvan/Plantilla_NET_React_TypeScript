# Multi-tenancy

Un **tenant** es la frontera de aislamiento: un cliente con sus datos, usuarios, configuración y, opcionalmente, su
propia base de datos. No confundir con **unidad organizativa** (empresa, marca o sucursal dentro de un tenant).

## Modos

| `Tenancy:Mode` | Datos de negocio | Resolución de tenant | Uso |
|---|---|---|---|
| `None` | Base única, tenant implícito `default` | No hay | Una sola organización |
| `SharedDatabase` | Base compartida, filtro `TenantId` | Dominio, subdominio o token | SaaS con muchos clientes |
| `DatabasePerTenant` | Base propia por tenant (`Isolation=Dedicated`) | Igual | Clientes regulados |
| `Hybrid` | Compartida o dedicada según el tenant | Igual | Mezcla de planes |

Los módulos de plataforma (`Tenancy`, `Identity`) siempre viven en la base de plataforma; los de negocio usan la base
del tenant.

## Resolución

1. `Host`: el dominio de la petición está registrado en el tenant (`erp.acme.com`).
2. `Subdomain`: `acme.<RootDomain>` resuelve el tenant de código `acme`.
3. `Claim`: el claim `tid` del token.
4. `Header`: `X-Tenant: acme` (solo si se agrega a `Resolvers`, para servicios internos).

Si el dominio y el token apuntan a tenants distintos la respuesta es 403 `tenant.mismatch`. Un tenant suspendido
recibe 403 `tenant.suspended`. Un endpoint que requiere tenant sin resolverlo recibe 400 `tenant.not_resolved`.

## Usuarios en varios tenants

Los usuarios son globales. `TenantMembership` indica a qué tenants pertenecen. Al iniciar sesión se usa el tenant del
dominio o la membresía por defecto; `POST /api/v1/identity/auth/switch-context` emite un token para otro tenant (la SPA
muestra el selector si el usuario tiene más de uno). El administrador de plataforma (rol `SuperAdmin`) puede entrar a
cualquier tenant.

## Crear y aprovisionar tenants

Desde la pantalla *Tenants* o la API (`/api/v1/tenancy/tenants`):

1. Crear con código, nombre, dominio y aislamiento.
2. Para `Dedicated`: motor (`DatabaseProvider`) y referencia del secreto (`ConnectionSecretRef`). La cadena real se lee de
   `Secrets:<referencia>` mediante `ISecretStore` (reemplazable por Key Vault u otro almacén). La API nunca devuelve
   la cadena ni la referencia.
3. *Aprovisionar*: crea la base (si el motor lo permite), aplica migraciones de los módulos de negocio y ejecuta las
   semillas del tenant (roles, tipos de unidad). Equivale a `dotnet run --project src/Host/Project.MigrationService -- provision-tenant <codigo>`.

## Garantías de aislamiento

- Filtro global `TenantId` en toda entidad `IMultiTenant` cuando la base es compartida.
- El pipeline de guardado asigna el `TenantId` (ignora el del cuerpo) y rechaza modificar filas de otro tenant.
- Claves de caché con prefijo del tenant; el outbox despacha cada evento con el tenant que lo generó.
- `ScopedSql` obligatorio para SQL crudo.
- Pruebas: `TenantIsolationTests` y `DedicatedTenantTests` en los tres motores bloquean el pipeline.

## Límites conocidos

- Con cientos de tenants dedicados, ajusta el tamaño de pool en sus cadenas de conexión o usa un proxy (PgBouncer).
- Mover un tenant de compartido a dedicado requiere copiar sus datos (no automatizado).
