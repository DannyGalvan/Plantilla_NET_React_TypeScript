# ADR 0002 — Mediator (martinothamar) con source generator

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** los casos de uso del backend deben vivir en clases únicas
  para SRP. Un bus interno de mensajes simplifica el pipeline (logging →
  validation → authorization → caching → transaction).
- **Decisión:** `Mediator.Abstractions` + `Mediator.SourceGenerator` de
  `martinothamar`. El source generator evita reflexión y soporta `sealed`
  parcial. Los handlers abiertos (genéricos sin cerrar) no se soportan
  bien; el CRUD genérico de Fase 3 vive en `ICrudService` por esa razón.
- **Consecuencias:**
  - Pipelines componibles vía `IPipelineBehavior`.
  - Handlers `sealed`, validación con FluentValidation por assembly scan.
  - El proyecto que define handlers debe ser el único que referencia
    `Mediator.SourceGenerator` para evitar el conflicto CS0436 que vimos en
    el proyecto de arquitectura.
- **Alternativas descartadas:** MediatR (licencia comercial), Wolverine
  (más invasivo), bus custom (reinventar la rueda).