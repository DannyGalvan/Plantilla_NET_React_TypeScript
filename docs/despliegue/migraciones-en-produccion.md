# Migraciones en producción

> Tres caminos. La plantilla los soporta; elige según tu criterio operativo.

## A. `Project.MigrationService` (recomendado)

Worker dedicado. Aplica migraciones en orden:

1. Contextos `DataScope=Platform` (Tenancy, Identity) contra la BD de
   plataforma.
2. Contextos `DataScope=Tenant`:
   - `None` / `SharedDatabase`: una sola vez.
   - `DatabasePerTenant`: por cada tenant activo, en paralelo con
     `Migrations:MaxParallelTenants`.
   - `Hybrid`: cada tenant según su catálogo (`Shared` vs `Dedicated`).

Si un tenant falla, sigue con los demás y devuelve un reporte con código
de salida != 0.

```bash
dotnet run --project src/Host/Project.MigrationService -- migrate
dotnet run --project src/Host/Project.MigrationService -- migrate --tenant acme
dotnet run --project src/Host/Project.MigrationService -- provision-tenant acme
dotnet run --project src/Host/Project.MigrationService -- move-tenant acme --to dedicated
dotnet run --project src/Host/Project.MigrationService -- reseed
```

## B. `dotnet ef migrations bundle`

Genera un ejecutable autocontenido con todas las migraciones aplicadas:

```bash
dotnet ef migrations bundle \
  --project src/Migrations/Project.Migrations.SqlServer/Project.Migrations.SqlServer.csproj \
  --output ./efbundle.exe \
  --force
```

Sube `./efbundle.exe` al servidor y ejecútalo antes de desplegar el API.
Útil cuando el equipo de DBAs quiere revisar el SQL antes de aplicar.

## C. Script SQL idempotente

```bash
dotnet ef migrations script --idempotent --output migrate.sql
```

Para DBAs que exigen revisar el SQL antes de aplicarlo a mano.

## Lock distribuido

El `MigrationService` toma un lock distribuido antes de empezar:

| Motor | Lock |
|---|---|
| SQL Server | `sp_getapplock` con timeout configurable |
| PostgreSQL | `pg_advisory_lock` con `pg_try_advisory_lock` + retry |
| MySQL | `GET_LOCK` con timeout configurable |

Si el lock no se obtiene, el worker falla rápido con código de salida != 0
y un mensaje claro. Ver [`docs/troubleshooting.md`](../troubleshooting.md)
para los errores más comunes.

## Multi-tenant

```bash
# Modo None o SharedDatabase: una sola pasada.
dotnet run --project src/Host/Project.MigrationService -- migrate

# DatabasePerTenant: una pasada por tenant.
dotnet run --project src/Host/Project.MigrationService -- migrate --tenant acme

# Hybrid: cada tenant en su BD, con --max-parallel para acotar.
dotnet run --project src/Host/Project.MigrationService -- migrate --max-parallel 4

# Provisionar un tenant nuevo (crea BD si el motor lo permite, luego migra y siembra).
dotnet run --project src/Host/Project.MigrationService -- provision-tenant globex

# Mover un tenant entre Shared y Dedicated (copia por módulo, ventana de mantenimiento).
dotnet run --project src/Host/Project.MigrationService -- move-tenant acme --to dedicated
```

## Compatibilidad hacia atrás

Patrón **expand/contract** para despliegues sin caída:

1. **Expand**: agregar columnas nullable / tablas / endpoints. La versión
   vieja del API sigue funcionando.
2. **Deploy**: subir la nueva versión. Coexisten.
3. **Migrate**: el `MigrationService` corre las migraciones. La versión vieja
   del API ignora las columnas nuevas.
4. **Contract**: el código nuevo deja de leer las columnas viejas.
5. **Cleanup**: eliminar las columnas ya no usadas.

Documenta cada paso en el CHANGELOG. Nunca borres columnas en el mismo
deploy que las dejas de leer.