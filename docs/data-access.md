# Data access

> Dónde usar EF Core, Dapper, stored procedures, bulk, y cómo respetar la
> multi-tenancy.

## Reglas

- **Escrituras**: EF Core a través de `IRepository<T, TId>` /
  `IUnitOfWork`. Las transacciones se aboman via `TransactionBehavior`
  en `Mediator`.
- **Lecturas**: EF con `AsNoTracking` + proyección Mapperly, **o** Dapper/SP
  a través de `ISqlExecutor` para reportes, listados masivos y bulk.
- **Conexión compartida**: `DapperSqlExecutor<TContext>` reusa la conexión
  (y transacción) del DbContext cuando hay una activa; si no, abre
  conexión pooled vía `IDbConnectionFactory`.

## Multi-tenancy

- `ModuleDbContext` combina filtros `IMultiTenant` + `IOrganizationScoped`
  + `ISoftDeletable` en un único `HasQueryFilter` por entidad.
- En `SharedDatabase`/`Hybrid-shared`, el filtro `TenantId` aplica. En
  `DatabasePerTenant` no.
- `TenantInterceptor` asigna el tenant actual en `Inserted` y lanza
  `TenantMismatchException` si un `Modified` trae un `TenantId` distinto.
- **Nunca** llames `IgnoreQueryFilters()` fuera de `Project.Infrastructure`
  o del módulo `Tenancy`. Lo verifica `ArchitectureTests`.

## Multi-proveedor

`IDatabaseProvider` strategy (OCP). Implementaciones:

- `SqlServerProvider` (Microsoft.Data.SqlClient, retries, migrations assembly).
- `PostgreSqlProvider` (Npgsql, snake_case, `pg_advisory_lock`).
- `MySqlProvider` (MySql.EntityFrameworkCore de Oracle — fallback de Pomelo).

El dialect genera `QuoteIdentifier`, `ParameterPrefix`, `CallProcedureSyntax`
y `SkipLockedHint`. La selección es por `Database:Provider`.

## Bulk

- `ExecuteUpdateAsync`/`ExecuteDeleteAsync` de EF 7+ solo dentro de
  `Project.Infrastructure`. **Prohibido** en módulos de negocio (el
  architecture test lo enforza).
- `SqlBulkCopy` (SQL Server) o `COPY` (PostgreSQL) vía `IBulkWriter`
  opcional para inserciones masivas.