# Acceso a datos

## Cuándo usar cada vía

| Necesidad | Vía |
|---|---|
| Escrituras, CRUD, reglas de dominio | EF Core con `IRepository<T,TId>` y `IUnitOfWork` |
| Listados con filtros del usuario | CRUD genérico o `Specification<T>` + proyección (`ProjectToDto`) |
| Reportes, agregaciones, listados masivos | Dapper con `ISqlExecutor<TModulo>` y `ScopedSql` |
| Lógica existente en la base | Procedimientos almacenados con `QueryProcedureAsync` |
| Actualizaciones masivas | `ExecuteUpdateAsync` / `ExecuteDeleteAsync` de EF Core en la infraestructura del módulo |

EF Core y Dapper comparten la conexión del módulo; si hay una transacción abierta, Dapper participa en ella.

## Motores

Cada motor es una clase `IDatabaseProvider` (`src/BuildingBlocks/Project.Infrastructure/Persistence/Providers`) que
configura EF Core, crea conexiones ADO.NET, define el dialecto SQL y el bloqueo distribuido de migraciones.

| Motor | Esquemas | Nombre de tabla | Procedimientos |
|---|---|---|---|
| PostgreSQL | Sí (`catalog.Products`) | PascalCase entre comillas | Funciones (`SELECT * FROM fn(...)`) |
| SQL Server | Sí (`[catalog].[Products]`) | PascalCase | `CommandType.StoredProcedure` |
| MySQL | No: prefijo (`catalog_Products`) | PascalCase | `CALL` |

Agregar un motor: nueva clase `IDatabaseProvider`, registrarla en `AddInfrastructure`, nuevo proyecto
`Project.Migrations.<Motor>` con factorías de diseño y su fila en `scripts/add-migration`.

## Filtros globales

`ModuleDbContext` aplica filtros con nombre a toda entidad según sus marcadores:

| Marcador | Filtro | Cómo omitirlo |
|---|---|---|
| `ISoftDeletable` | `!IsDeleted` | `Specification.WithDeleted()` |
| `IMultiTenant` | `TenantId = tenant actual` (solo base compartida) | No se omite desde módulos |
| `IOrganizationScoped` | Unidad dentro de las accesibles del usuario | Permiso `organization.scope.all` sin unidad activa |

Los valores se leen de propiedades del contexto, así que EF Core los envía como parámetros y el modelo cacheado sirve
para todos los tenants.

## SQL crudo seguro

Los filtros globales no aplican a Dapper. Usa `ScopedSql`:

```csharp
var scope = ScopedSql.For(sql.Dialect, tenant, organization);
var text = $"""
    SELECT {scope.Column("p", "Name")} AS Name
    FROM {sql.Dialect.Table("catalog", "Products")} p
    WHERE {scope.Tenant("p")} AND {scope.NotDeleted("p")} AND {scope.Units("p")}
    """;
var rows = await sql.QueryAsync<Row>(text, scope.Parameters(new { Threshold = 5 }), ct);
```

Mapea resultados de Dapper a clases con propiedades (no records posicionales): los motores devuelven tipos numéricos
distintos para `COUNT` y `SUM`.

## Procedimientos almacenados

Créalos con una migración vacía por motor y SQL propio de cada uno:

```bash
for p in SqlServer PostgreSql MySql; do
  (cd src/Migrations/Project.Migrations.$p && dotnet ef migrations add MyProcedure --context CatalogDbContext --output-dir Catalog)
done
```

Ejemplo completo: `*_LowStockProcedure.cs` en cada proyecto de migraciones y `GetLowStockHandler`.

## Migraciones

- Una carpeta por módulo dentro de cada `Project.Migrations.<Motor>`.
- `./scripts/add-migration.sh <Modulo> <Nombre>` genera la migración en los tres motores.
- CI ejecuta `dotnet ef migrations has-pending-model-changes` para cada motor y módulo.
- La API nunca migra en Production; usa el worker (ver [migraciones en producción](../despliegue/migraciones-en-produccion.md)).
