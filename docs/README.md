# Índice de documentación

> Mapa navegable por perfil. Elige tu punto de entrada según lo que necesites hacer.

## Soy nuevo en el proyecto

1. Lee [`docs/architecture.md`](architecture.md) — diagrama de bloques, capas y reglas de dependencia.
2. Lee [`docs/01-prerequisitos.md`](01-prerequisitos.md) — herramientas requeridas por SO.
3. Sigue [`docs/02-inicio-rapido.md`](02-inicio-rapido.md) — clone, build, primer arranque.
4. Si algo falla, [`docs/troubleshooting.md`](troubleshooting.md).

## Voy a crear una aplicación

1. [`docs/06-crear-una-aplicacion.md`](06-crear-una-aplicacion.md) — árbol de decisión y recetas por perfil (`simple` / `erp` / `saas` / `saas-dedicated`).
2. [`docs/03-configuracion-inicial.md`](03-configuracion-inicial.md) — checklist de día 1 (secretos, conexión, marca, idioma, migraciones).
3. [`docs/04-ejecucion-local.md`](04-ejecucion-local.md) — cómo correr backend + frontend, hot reload, tests.

## Voy a desplegar

1. [`docs/despliegue/00-estrategias.md`](despliegue/00-estrategias.md) — comparativa del topológico.
2. [`docs/despliegue/docker.md`](despliegue/docker.md) — Docker / Compose con la imagen multi-stage.
3. [`docs/despliegue/iis-windows-server.md`](despliegue/iis-windows-server.md) — IIS con el Hosting Bundle .NET 10.
4. [`docs/despliegue/linux-systemd-nginx.md`](despliegue/linux-systemd-nginx.md) — Linux + systemd + nginx/caddy.
5. [`docs/despliegue/azure-app-service.md`](despliegue/azure-app-service.md) — App Service (opcional).
6. [`docs/despliegue/kubernetes.md`](despliegue/kubernetes.md) — manifests de ejemplo (probes, HPA).
7. [`docs/despliegue/migraciones-en-produccion.md`](despliegue/migraciones-en-produccion.md) — `MigrationService` vs `efbundle` vs SQL idempotente.
8. [`docs/despliegue/checklist-produccion.md`](despliegue/checklist-produccion.md) — lista marcable pre-producción.

## Voy a extender la plataforma

1. [`docs/07-agregar-un-modulo.md`](07-agregar-un-modulo.md) — paso a paso backend + frontend.
2. [`docs/08-tecnologias.md`](08-tecnologias.md) — versiones y licencias por capa.
3. [`docs/guias/permisos-y-navegacion.md`](guias/permisos-y-navegacion.md) — declarar permisos y navegación.
4. [`docs/guias/theming-y-marca.md`](guias/theming-y-marca.md) — agregar preset de marca.
5. [`docs/adr/`](adr/) — decisiones arquitectónicas (no reabrir sin consultar antes).

## Quiero saber más sobre un tema específico

- **Acceso a datos**: [`docs/guias/acceso-a-datos.md`](guias/acceso-a-datos.md).
- **Multi-tenancy**: [`docs/guias/multitenancy.md`](guias/multitenancy.md).
- **Organización**: [`docs/guias/organizacion.md`](guias/organizacion.md).
- **Seguridad**: [`docs/guias/seguridad.md`](guias/seguridad.md) y [`docs/security.md`](security.md).
- **i18n**: [`docs/guias/i18n.md`](guias/i18n.md).
- **Testing**: [`docs/guias/testing.md`](guias/testing.md).
- **Escalabilidad**: [`docs/guias/escalabilidad.md`](guias/escalabilidad.md).
- **Observabilidad**: [`docs/guias/observabilidad.md`](guias/observabilidad.md).
- **Background jobs y eventos**: [`docs/guias/background-jobs-y-eventos.md`](guias/background-jobs-y-eventos.md).
- **Glosario**: [`docs/glosario.md`](glosario.md).

## Reglas y decisiones

- [`agents.md`](../agents.md) — convenciones obligatorias (backend + frontend).
- [`docs/adr/`](adr/) — decisiones arquitectónicas inmutables salvo nueva ADR.
- [`plans/2026-10-10_refactor-plantilla-modular-empresarial.md`](../plans/2026-10-10_refactor-plantilla-modular-empresarial.md) — plan maestro y criterios de salida por fase.