# Troubleshooting

> Errores frecuentes por SO y por motor. Si tu problema no está aquí,
> abre un issue con la salida completa de `scripts/doctor` y el comando
> que disparó el error.

## Backend

### `dotnet ef migrations has-pending-model-changes` reporta cambios pendientes

El modelo y la última migración no coinciden. Genera una migración
nueva:

```bash
./scripts/add-migration.sh <Module> <Name>
```

### `AddRefreshTokens` no se descubre

`Project.Server/Migrations/AddRefreshTokens.Designer.cs` no existe en la
carpeta del proyecto. La migración está **huérfana**. Borra `Migrations/` y
regenera con `add-migration.sh InitialCreate RefreshTokens`.

### `sp_getapplock` falla con timeout en SQL Server

El lock distribuido por `sp_getapplock` tiene timeout por defecto de
22s. Si tu `MigrationService` corre con varios workers en paralelo y
uno se cuelga, los demás esperan. Aumenta el timeout o usa `LockTimeout = 0`
para "fail fast":

```csharp
migrationBuilder.Sql("EXEC sp_getapplock @Resource = 'plantilla', @LockMode = 'Exclusive', @LockTimeout = 5000;");
```

### `pg_advisory_lock` queda colgado en PostgreSQL

`pg_advisory_lock` es por sesión. Si la sesión muere sin `pg_advisory_unlock`,
el lock persiste hasta `pg_terminate_backend`. En CI, usa
`pg_try_advisory_lock` con retry corto y abandona el proceso.

### `MySql.EntityFrameworkCore` lanza `MySqlConnector.MySqlException: Access denied`

La cadena de conexión tiene usuario sin permisos sobre la BD. Asegúrate de
que el usuario tiene `CREATE`, `ALTER`, `DROP` (migraciones) y los permisos
de lectura/escritura por tabla.

### JWT firma con error `IDX10503: Signature validation failed`

`Jwt:SigningKey` cambió entre el emisor y el validador. Si tienes varias
instancias detrás de un balanceador, todas deben leer la misma clave.
Para rotación gradual, emite con `IssuerSigningKey = Key1` y valida con
`IssuerSigningKey = Key1, Key2`.

## Frontend

### `pnpm verify` falla con `prettier` complaining

```bash
cd project.client
pnpm format
pnpm verify
```

### `pnpm dev` no proxy al backend

Vite lee `vite.config.ts`. La regla `server.proxy` debe apuntar al API.
Con `VITE_API_URL=` vacío (mismo origen), el proxy es
`https://localhost:5050`. Si cambias `VITE_API_URL`, también cambia el
proxy.

### CSP bloquea un script inline

Las páginas que necesitan `<script>` deben usar `nonce` o mover el script
a un bundle. Los componentes UI no insertan scripts inline; si aparece,
es un antipatrón.

### Token de sesión se borra al refrescar la página

El access token vive en memoria. Si esperabas que sobreviviera al
refresh, falta el bootstrap `auth/session.ts` con `useEffect` que
llama a `POST /identity/auth/refresh` con `credentials: 'include'`. La
cookie HttpOnly persiste, así que el bootstrap rehidrata el store.

## IIS / Windows Server

### 500.30 — ANCM In-Process启动失败

Falta el **ASP.NET Core Hosting Bundle**. Reinicia IIS tras instalarlo:

```powershell
net stop was /y
net start w3svc
```

### 502.5 — Process Failure

`ASPNETCORE_FORWARDEDHEADERS_ENABLED` o el módulo ANCM no está
configurado. Verifica que `web.config` incluye
`<handlers><add name="aspNetCore" ... /></handlers>`.

### Permisos denegados al escribir logs

El App Pool corre bajo `ApplicationPoolIdentity` por defecto. Otorga
permisos de lectura/escritura a la carpeta de logs:

```powershell
icacls "C:\inetpub\wwwroot\plantilla\logs" /grant "IIS AppPool\DefaultAppPool:(OI)(CI)(M)"
```

## Linux + systemd

### 502 Bad Gateway tras reiniciar nginx

`proxy_pass http://127.0.0.1:5000` apunta al puerto equivocado. Si
publicaste en `http://localhost:5050` con `--urls`, cambia el puerto
en `proxy_pass`.

### `systemd` mata el servicio con `OOMKilled`

`MemoryMax=` no está configurado y systemd deja al servicio ilimitado.
Configúralo en la unit:

```ini
[Service]
MemoryMax=512M
```

## Kubernetes

### CrashLoopBackOff en el primer arranque

`livenessProbe` falla antes de que la app esté lista. Usa `initialDelaySeconds`
≥ 30 y `failureThreshold` ≥ 5:

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10
  failureThreshold: 5
```

### `ConnectionStrings__Default` no se inyecta desde un Secret

`envFrom` en el Deployment requiere que el Secret exista antes del
arranque. Si el Secret se crea después del primer rollout, el pod falla.
Asegúrate de que el Secret existe en el namespace y que el ServiceAccount
tiene `get secrets` permitido.

### OutboxProcessor no procesa mensajes

El pod `MigrationService` debe correr con permiso para `EXECUTE` /
`UPDATE` / `DELETE` en la tabla `OutboxMessages`. Si la BD es
`DatabasePerTenant`, el catálogo de tenants debe estar cargado antes
del primer poll. Logs en `kubectl logs ` para `Outbox tick`.

## GitHub Actions

### `dotnet ef` falla con `command not found`

El job no tiene `dotnet-ef` instalado. Añade:

```yaml
- name: Restore .NET tools
  run: dotnet tool restore
```

### `pnpm install` falla con `ECONNRESET`

Cache corrupto. Borra `~/.npm/_cacache` y vuelve a correr.

### Backend test pasa local pero falla en CI con timeout

`func-testcontainers` levanta contenedores pesados. Si el runner no
tiene suficiente RAM, los contenedores mueren. Limita el parallelism
con `[CollectionDefinition("Database", DisableParallelization = true)]`.

Si tu problema no está aquí, [`docs/faq.md`](faq.md) tiene preguntas
frecuentes adicionales. Si sigues atascado, abre un issue con la salida
de `scripts/doctor`.