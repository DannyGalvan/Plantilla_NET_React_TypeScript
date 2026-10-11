# ADR 0006 — Tenancy configurable por escenario (matriz §3.4)

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** una sola base debe cubrir desde una app de inventario de
  una sola empresa hasta un SaaS multi-tenant con bases dedicadas. Esto no
  se puede resolver con un único modo fijo.
- **Decisión:** `Tenancy:Mode` admite `None`, `SharedDatabase`,
  `DatabasePerTenant`, `Hybrid`. Cada módulo declara `DataScope` (Platform o
  Tenant). El módulo `Tenancy` (Platform) mantiene el catálogo y los
  tenants; el módulo `Organization` (Tenant, opcional) filtra por
  `AccessibleUnitIds`.
- **Consecuencias:**
  - `ITenantContext` + `ITenantSetter` + `ITenantResolver` en `Project.Infrastructure`.
  - `ModuleDbContext` aplica el filtro `TenantId` solo cuando la BD es
    compartida.
  - `OrganizationContext` aplica `AccessibleUnitIds.Contains(...)` cuando
    el módulo Organization está registrado.
- **Alternativas descartadas:** multi-tenant con schema por tenant (más
  complejo, no portable), una BD por tenant fija (no cubre single-tenant).