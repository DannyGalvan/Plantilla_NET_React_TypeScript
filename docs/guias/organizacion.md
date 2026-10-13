# Organización y settings en cascada

## Unidades organizativas

El módulo opcional `Organization` modela la jerarquía de negocio dentro de un tenant: empresa, país, marca, sucursal o
los tipos que definas en *Tipos de unidad* (cada tipo indica si puede ser raíz y qué tipos pueden contenerlo).

Cada unidad guarda su ruta materializada (`/raiz/hijo/nieto/`), lo que permite consultar descendientes con un índice
en cualquier motor. Mover una unidad reescribe la ruta de sus descendientes en la misma transacción.

## Quién ve qué

- Un usuario asignado a una unidad ve sus datos y los de sus descendientes.
- Con una unidad activa (selector de la barra superior) ve solo esa rama.
- Con el permiso `organization.scope.all` y sin unidad activa ve todo el tenant.
- Los datos `IOrganizationScoped` reciben la unidad activa al crearse; escribir en una unidad fuera del alcance devuelve 403.
- Los roles se pueden asignar en una unidad (`POST /api/v1/identity/users/{id}/role-assignments`); aplican en esa unidad
  y sus descendientes.

Sin el módulo, `IOrganizationContext` es nulo: no hay filtro ni selector.

## Settings en cascada

Cada sección (`localization`, `branding`, `tax`, `security` y las que declaren los módulos) se resuelve en este orden,
ganando el valor más específico por propiedad:

1. Valores por defecto en código.
2. `appsettings.json` (`Settings:<seccion>`).
3. Sistema (administrador de plataforma).
4. Tenant.
5. Unidades desde la raíz hasta la activa (por ejemplo empresa, país, marca).
6. Usuario (solo secciones que lo permiten, como `localization`).

La pantalla *Configuración* se genera a partir del esquema que expone `GET /api/v1/tenancy/settings/schema`, así que una
sección nueva no necesita UI. Los colores de marca se validan en el servidor (hex u OKLCH).

Ejemplo: cambiar `localization.currency` en la unidad *Guatemala* hace que todas las marcas y sucursales bajo ese país
muestren quetzales, mientras el resto del grupo mantiene su moneda.
