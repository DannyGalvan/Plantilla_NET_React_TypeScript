# ADR 0001 — Monolito modular con Clean Architecture por módulo

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** la plantilla sirve tanto para una app de inventario de un
  solo tenant como para un SaaS multi-tenant. Necesitamos un solo build que
  cubra ambos extremos sin cambiar código de negocio.
- **Decisión:** monolito desplegable con un proyecto web host
  (`src/Host/Project.Api`) que carga módulos vía `IModule`. Cada módulo tiene
  cuatro proyectos (`Domain`, `Application`, `Infrastructure`, `Contracts`).
  Los BuildingBlocks (`SharedKernel`, `Application`, `Infrastructure`, `Web`)
  concentran los contratos transversales.
- **Consecuencias:**
  - Un solo binario desplegable. Escala horizontal replicando instancias.
  - Pruebas de aislamiento por modo de tenancy en `tests/Project.Api.FunctionalTests`.
  - El monolito se descompone en servicios cuando un módulo lo necesite
    (vía contratos + outbox). El refactor es interno al módulo.
- **Alternativas descartadas:** microservicios (overhead operativo no
  justificado para una plantilla), modular monolith sin contratos entre
  módulos (acopla por reflection y rompe OCP).