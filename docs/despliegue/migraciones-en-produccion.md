# Migraciones en producción

La API nunca migra en `Production` (si `Database:MigrateOnStartup=true` no arranca). Las migraciones las aplica el worker
`Project.MigrationService`, que termina al acabar.

## Comandos

```bash
dotnet Project.MigrationService.dll migrate                     # plataforma + tenants
dotnet Project.MigrationService.dll migrate --tenant acme       # solo un tenant
dotnet Project.MigrationService.dll provision-tenant acme       # crea, migra y siembra un tenant dedicado
```

En cada ejecución:

1. Toma un bloqueo distribuido en la base (`sp_getapplock`, `pg_advisory_lock` o `GET_LOCK`): dos ejecuciones
   simultáneas no se pisan.
2. Migra los módulos de plataforma, sincroniza permisos y ejecuta las semillas de plataforma.
3. Migra la base compartida de los módulos de negocio y, por cada tenant dedicado, su propia base con paralelismo
   `Migrations:MaxParallelTenants`. Un tenant que falla no detiene a los demás; el código de salida es distinto de 0.

## Alternativa: script SQL para revisión del DBA

```bash
cd src/Migrations/Project.Migrations.SqlServer
dotnet ef migrations script --idempotent --context CatalogDbContext -o catalog.sql
```

## Cambios sin caída

Usa expandir y contraer: primero agrega columnas o tablas compatibles con la versión anterior, despliega, migra datos
y, en una versión posterior, elimina lo viejo. Así un rollback de la API no requiere revertir la base.
