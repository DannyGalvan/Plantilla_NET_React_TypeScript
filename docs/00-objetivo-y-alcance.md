# Objetivo y alcance

**Para qué sirve este documento:** entender qué problema resuelve la plantilla, qué incluye y qué no.
**Tiempo estimado:** 5 minutos.

## Problema

Cada aplicación de negocio repite la misma base: usuarios, roles y permisos, sesiones seguras, separación de datos
por cliente o por empresa, configuración por marca, auditoría, acceso a datos eficiente, pruebas y despliegue. La
plantilla entrega esa base terminada y probada para que el equipo dedique su tiempo a los módulos de negocio.

## Principios

- **Abierto/cerrado:** agregar un módulo no modifica el código existente (solo una línea en `AppModules`).
- **Responsabilidad única:** un caso de uso por clase; infraestructura, dominio y presentación separados.
- **Seguro por defecto:** endpoints sin declaración de seguridad impiden el arranque; datos aislados por filtros globales.
- **Configurable:** topología, motor de base de datos, módulos y marca se eligen por configuración.
- **Sin costo por lo que no se usa:** en modo `None` no hay resolución de tenant ni filtro organizativo.

## Escenarios soportados con el mismo código

| Escenario | `Tenancy:Mode` | Módulo Organization | Ejemplo |
|---|---|---|---|
| Una empresa, una marca | `None` | quitado | Tienda, inventario interno |
| Grupo con varias empresas, marcas o países | `None` | activo | ERP de un holding |
| SaaS con muchos clientes en una base | `SharedDatabase` | opcional | CRM para pymes |
| SaaS con clientes que exigen base propia | `DatabasePerTenant` | opcional | Sector financiero o salud |
| Mezcla de los dos anteriores | `Hybrid` | opcional | Plan estándar compartido y plan enterprise dedicado |

## Qué incluye

- Plataforma backend reutilizable (BuildingBlocks) y módulos de plataforma (Tenancy, Identity, Organization).
- Capacidades transversales para cualquier dominio: archivos (Drive, OneDrive, S3, Azure), notificaciones, historial de
  cambios, tareas programadas, numeración, `Money`, aprobaciones, importar/exportar y webhooks.
- Un módulo de ejemplo opcional (`Catalog`), activo solo en Development.
- SPA React con módulos equivalentes, sistema de diseño por tokens y personalización por marca y usuario.
- Migraciones para PostgreSQL, SQL Server y MySQL, worker de migraciones y aprovisionamiento de bases dedicadas.
- Pruebas unitarias, de arquitectura y funcionales contra los tres motores, y CI listo.
- Artefactos de despliegue para Docker, Linux e IIS.

## Qué no incluye

- No es una herramienta low-code: los módulos se escriben en C# y TypeScript (con generador de esqueleto).
- No incluye pasarela de pagos, facturación electrónica ni integraciones específicas de un país.
- No mueve datos de un tenant entre motores distintos (por ejemplo de PostgreSQL a SQL Server).
- No incluye un bus de mensajes externo; el bus en proceso se puede reemplazar (ver [eventos y jobs](guias/eventos-y-jobs.md)).

## Recibir actualizaciones de la plantilla

```bash
git remote add upstream https://github.com/DannyGalvan/Plantilla_NET_React_TypeScript.git
git fetch upstream
git merge upstream/master
```

Para que los merges sean simples, evita modificar `src/BuildingBlocks` y `src/project.client/src/shared`; extiende con
módulos propios.
