# ADR 0008 — Mapperly para mapeos

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** el CRUD genérico necesita mapear entidades a DTO, también como proyección `IQueryable`, sin reflexión en tiempo de ejecución.
- **Decisión:** `Riok.Mapperly` (Apache 2.0) genera el código de mapeo en compilación. Cada recurso declara un `[Mapper] partial class` que implementa `ICrudMapper`.
- **Consecuencias:**
  - Errores de mapeo detectados en compilación.
  - Proyección traducida a SQL por EF Core.
  - Los campos que no deben mapearse se marcan con `MapperIgnoreTarget`.
- **Alternativas descartadas:** AutoMapper (licencia comercial desde v15), Mapster (reflexión y configuración global), mapeo manual (repetitivo).
