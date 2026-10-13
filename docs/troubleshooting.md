# Solución de problemas

## Desarrollo

| Síntoma | Solución |
|---|---|
| El navegador rechaza `https://localhost:7180` o `5173` | `dotnet dev-certs https --clean` y `dotnet dev-certs https --trust`. En Linux confía el certificado exportado manualmente o usa `VITE_HTTPS=false` |
| `Jwt:SigningKey is not configured` al iniciar sesión | Falta `Configuration:MasterKey`: configúrala (`scripts/setup` o `Configuration__MasterKey`) y ejecuta las migraciones para que se genere la llave |
| *Configuración de plataforma* avisa que no hay llave maestra | Igual que arriba; sin master key no se pueden guardar secretos |
| Un secreto guardado dejó de funcionar tras cambiar de servidor | La `Configuration:MasterKey` es distinta: usa la original o vuelve a escribir los secretos |
| Un valor cambiado en la pantalla no se aplica | Una variable de entorno lo sobrescribe (la pantalla lo indica) o el campo requiere reiniciar |
| `Seed:AdminPassword is required` | Fuera de Development la contraseña inicial es obligatoria |
| No conecta a la base | Verifica `docker compose ps`, el perfil levantado y `Database:Provider` coincidente con la cadena |
| Puerto ocupado (7180, 5173, 5432, 1433, 3306) | Detén el proceso (`lsof -i :7180` / `Get-NetTCPConnection -LocalPort 7180`) o cambia el puerto en `launchSettings.json` |
| `The migrations ... already exist` / tablas duplicadas tras cambiar de rama | Base de desarrollo desfasada: elimina y vuelve a crear la base local |
| La SPA muestra la pantalla de error al iniciar | La API no está corriendo o el proxy apunta a otro puerto (`API_URL`) |
| 401 continuo tras iniciar sesión | Reloj del sistema desfasado o `Jwt:Issuer`/`Audience` distintos entre instancias |
| La API no arranca: "Endpoints without an explicit security declaration" | Un endpoint nuevo no declara `RequirePermission`, `RequireAuthenticatedUser` o `AllowAnonymous` |
| Las pruebas funcionales fallan con "Docker is not running" | Inicia Docker; Testcontainers lo necesita |
| MySQL: "Specified key was too long" | Un `string` indexado sin `HasMaxLength` o demasiado largo para utf8mb4 (máximo 3072 bytes por índice) |

## Docker

| Síntoma | Solución |
|---|---|
| `api` no inicia y `migrations` terminó con error | `docker compose ... logs migrations`; normalmente secretos vacíos o `ADMIN_PASSWORD` que no cumple la política |
| `variable is not set` al levantar | Falta `--env-file deploy/docker/.env` o el archivo no existe |
| Healthcheck en `unhealthy` | `docker compose ... logs api`; revisa la conexión a `db` y `redis` |

## Linux

| Síntoma | Solución |
|---|---|
| `status=203/EXEC` | Ruta de `dotnet` distinta: `which dotnet` y ajusta `ExecStart` |
| 502 en nginx | La API no escucha en `127.0.0.1:5000`; `journalctl -u project-api` |
| IP del cliente siempre `127.0.0.1` en logs y rate limit | Falta `Hosting__KnownProxies__0=127.0.0.1` |

## IIS

| Síntoma | Solución |
|---|---|
| 500.19 | Falta el Hosting Bundle; reinstala y reinicia IIS |
| 500.30 | Error de arranque: activa `stdoutLogEnabled`, revisa `logs\stdout*` y el Visor de eventos |
| Los eventos del outbox se procesan con retraso | El App Pool no está en `AlwaysRunning` o tiene tiempo de inactividad |

## Producción

| Síntoma | Solución |
|---|---|
| Las sesiones se pierden al cambiar de instancia | Todas las instancias deben usar la misma base de plataforma (la llave JWT vive ahí) y Redis compartido |
| La cookie de refresh no se envía | La SPA está en otro sitio (`SameSite=Strict`); sírvela desde la API o un subdominio del mismo sitio |
| `tenant.mismatch` (403) | El token es de otro tenant que el dominio usado; inicia sesión en el dominio correcto |
