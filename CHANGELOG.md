# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versionado [SemVer](https://semver.org/lang/es/).

## [2.0.0] - 2026-10-11

Reescritura completa de la plantilla como plataforma modular.

### Agregado

- Monolito modular en .NET 10 con BuildingBlocks (`SharedKernel`, `Application`, `Infrastructure`, `Web`) y módulos
  `Tenancy`, `Identity`, `Organization` y `Catalog`, cada uno con `Domain`, `Application`, `Infrastructure` y `Contracts`.
- Motor CRUD genérico con filtros parametrizados por allowlist, orden, paginación, JSON Merge Patch, borrado lógico,
  restauración y concurrencia optimista.
- Persistencia multi-motor (PostgreSQL, SQL Server, MySQL), Dapper con el mismo contexto transaccional, procedimientos
  almacenados, outbox y worker de migraciones con bloqueo distribuido.
- Multi-tenancy `None`, `SharedDatabase`, `DatabasePerTenant` y `Hybrid`, con aprovisionamiento de bases dedicadas.
- Estructura organizativa con filtrado de datos por unidad y roles por unidad.
- Settings en cascada, feature flags por tenant y `/api/v1/platform/config` resuelto por host.
- Frontend nuevo: módulos por manifiesto, tokens OKLCH, presets de marca, personalizador de tema, i18n es/en.
- Docker, systemd + nginx, IIS, CI con pruebas en los tres motores y smoke test de contenedores.

### Cambiado

- Respuestas de error unificadas en ProblemDetails (RFC 9457); identificadores Guid v7.
- Permisos resueltos en el servidor con caché; el JWT ya no contiene permisos.

### Eliminado

- Proyecto `Project.Server` (controllers MVC, servicios monolíticos) y su frontend anterior.
- Migraciones anteriores: no hay ruta de actualización para bases creadas con la versión 1.x.
