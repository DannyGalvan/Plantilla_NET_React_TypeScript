# ADR 0010 — Estrategia de pruebas

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** el mayor riesgo de la plataforma es la fuga de datos entre tenants o unidades y las diferencias entre motores.
- **Decisión:** pruebas unitarias para la lógica pura, pruebas de arquitectura con NetArchTest para las reglas de dependencia y pruebas funcionales con `WebApplicationFactory` y Testcontainers contra bases reales. CI ejecuta las funcionales en PostgreSQL, SQL Server y MySQL. El frontend usa Vitest, Testing Library y MSW.
- **Consecuencias:**
  - Los modos `None`, `SharedDatabase` y `Hybrid` (con base dedicada) se prueban contra la API real.
  - Las pruebas funcionales requieren Docker.
  - No se incluyen pruebas de navegador end-to-end; pueden agregarse con Playwright.
- **Alternativas descartadas:** base en memoria o SQLite (no reproduce filtros ni SQL de cada motor), mocks del DbContext (no prueban el aislamiento real).
