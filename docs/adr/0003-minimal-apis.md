# ADR 0003 — Minimal APIs agrupadas por módulo

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** la plantilla legacy usaba controllers MVC. Para los módulos
  nuevos queremos algo más cercano al routing del framework, menos
  ceremony por endpoint.
- **Decisión:** Minimal APIs agrupadas vía `RouteGroupBuilder`. Los módulos
  exponen `MapEndpoints(IEndpointRouteBuilder)` desde `IModule`. El builder
  `MapCrud` registra los 5 endpoints estándar (list/get/create/update/delete)
  con `RequirePermission(...)` metadata.
- **Consecuencias:**
  - Menos archivos boilerplate por endpoint.
  - Versionado con `Asp.Versioning.Http` (group version).
  - OpenAPI por `Microsoft.AspNetCore.OpenApi` + Scalar en Development.
  - Validación por FluentValidation decorando el `Command` directamente.
- **Alternativas descartadas:** Carter (exceso de magic), FastEndpoints
  (sobrecarga cognitiva), controllers MVC (lo que reemplaza el legacy).