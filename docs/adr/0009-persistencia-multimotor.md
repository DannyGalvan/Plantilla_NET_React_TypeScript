# ADR 0009 — Persistencia multi-motor con estrategias por proveedor

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** los proyectos derivados usan PostgreSQL, SQL Server o MySQL, y algunos tenants pueden requerir un motor distinto.
- **Decisión:** cada motor es una clase `IDatabaseProvider` con su configuración EF Core, conexión ADO.NET, dialecto SQL y bloqueo distribuido. Las migraciones viven en un proyecto por motor (`Project.Migrations.*`). En MySQL los esquemas se emulan con prefijo de tabla. Para MySQL se usa `MySql.EntityFrameworkCore` de Oracle porque Pomelo no tiene versión estable para EF Core 10.
- **Consecuencias:**
  - Agregar un motor no modifica código existente.
  - Cada cambio de modelo genera tres migraciones (`scripts/add-migration`) y CI verifica que no falten.
  - El SQL crudo usa el dialecto (`ISqlDialect`) para nombres de tabla y columnas.
- **Alternativas descartadas:** un solo proyecto de migraciones con condicionales (frágil), solo un motor (limita la adopción).
