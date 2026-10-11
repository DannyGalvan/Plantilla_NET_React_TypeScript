# 00 · Objetivo y alcance

## Qué es esta plantilla

Una **base empresarial modular** en .NET 10 LTS y React 19 + TypeScript, construida como un monolito desplegable, que sirve tanto para:

- una **app de inventario** de una sola empresa con una sola marca y una sola base, hasta
- un **SaaS multi-tenant** con bases dedicadas, varios idiomas, theming por marca y RBAC fino por organización.

El mismo build cubre ambos extremos. Lo que cambia entre un escenario y otro es **solo configuración y datos** (perfiles `simple` / `erp` / `saas` / `saas-dedicated`), no código de aplicación.

## Qué **no** es

- No es un **low-code**. Cada módulo es código .NET y React real; no se arrastran componentes.
- No trae **pasarela de pagos**, **storage de archivos**, ni **cliente de email transaccional**. Esos servicios externos se enchufan como adaptadores en `Project.Infrastructure`.
- `move-tenant` solo funciona entre motores **iguales**. Cruzar SqlServer ↔ PostgreSQL queda documentado como extensión.
- **No es SaaS** para casos donde cada cliente necesita **infraestructura dedicada con red aislada**. Para eso, despliega la plantilla por cliente y conecta SSO central.

## Principios

- **SOLID**: cada módulo es responsable de su agregado. `AuthService` (810 líneas) se reescribe como un handler por caso de uso en Fase 4.
- **OCP**: agregar un módulo = carpeta + un manifest. No se edita código central.
- **Seguro por defecto**: JWT, BCrypt, fail-closed en permisos, refresh cookie HttpOnly + CSRF, rate-limit distribuido, sin secretos en `localStorage`.
- **Sin sobrecarga en lo que no se usa**: `Tenancy:Mode = None` no agrega I/O ni filtros. `Organization` es opcional. `FeatureFlags` se cablea por tenant.
- **Todo configurable**: mediante `appsettings.json`, settings en cascada (`System → Tenant → OrgUnit → User`), variables de entorno y `user-secrets`.

## Escenarios soportados

| Escenario | `Tenancy:Mode` | Organización | Resultado |
|---|---|---|---|
| App pequeña, una empresa, una marca | `None` | no registrado | Tenant `default` sin I/O |
| ERP multi-empresa, multi-marca, multi-país | `None` | registrado | Una BD, datos filtrados por unidad |
| SaaS, muchos clientes, una BD | `SharedDatabase` | opcional | Filtro global por `TenantId`, índices compuestos |
| SaaS con clientes regulados o grandes | `DatabasePerTenant` | opcional | Una BD por tenant, motor elegible por tenant |
| Mixto (transición) | `Hybrid` | opcional | Cada tenant Shared o Dedicated según catálogo |

## Versionado y upgrades

- **Major**: breaking changes (cambio de IDs, contrato de respuesta). Se publica una ADR.
- **Minor**: nuevas features (módulo, política). Backwards-compatible.
- **Patch**: fixes, refactors sin cambio de comportamiento.

Para proyectos derivados: `git remote add upstream`, merge de `master`, ver [`plans/`](../plans/) para la hoja de ruta.