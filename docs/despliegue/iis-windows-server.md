# Despliegue en IIS (Windows Server)

**Para qué sirve este documento:** publicar la API en IIS.
**Requisitos previos:** Windows Server 2019 o superior con el rol Web Server, ASP.NET Core 10 Hosting Bundle.
**Tiempo estimado:** 30 minutos.

## 1. Preparar el servidor

1. Instala el rol Web Server (IIS).
2. Instala el **ASP.NET Core 10 Hosting Bundle** y reinicia IIS:

   ```powershell
   net stop was /y
   net start w3svc
   ```

## 2. Publicar (en la máquina de build)

```powershell
dotnet publish src/Host/Project.Api -c Release -o out\api
dotnet publish src/Host/Project.MigrationService -c Release -o out\migrations
```

La publicación incluye la SPA compilada y `web.config` (`deploy/iis/web.config`: modelo in-process, `Production`).

## 3. Crear el sitio

Copia `out\api` a `C:\sites\project` y ejecuta como administrador:

```powershell
./deploy/iis/Install-IisSite.ps1 -SitePath C:\sites\project -HostName erp.example.com -CertificateThumbprint <huella>
```

El script crea el App Pool sin código administrado, `AlwaysRunning`, sin tiempo de inactividad (necesario para el
outbox) y con precarga, crea el sitio con enlace HTTPS y concede permisos de lectura a la identidad del pool.

## 4. Secretos

Define variables de entorno del sistema (o en el editor de configuración de IIS, sección `environmentVariables`):

```powershell
[Environment]::SetEnvironmentVariable('Database__Provider', 'SqlServer', 'Machine')
[Environment]::SetEnvironmentVariable('Database__ConnectionStrings__Default', '<cadena>', 'Machine')
[Environment]::SetEnvironmentVariable('Jwt__SigningKey', '<clave-de-32-o-mas>', 'Machine')
[Environment]::SetEnvironmentVariable('Seed__AdminPassword', '<contraseña>', 'Machine')
iisreset
```

## 5. Migraciones

Antes de cada despliegue, desde `out\migrations` con las mismas variables:

```powershell
.\Project.MigrationService.exe migrate
```

## Cómo verificar que funcionó

```powershell
Invoke-RestMethod https://erp.example.com/health/ready
```

## Actualizar sin bloqueo de archivos

Copia `app_offline.htm` a la carpeta del sitio, reemplaza los archivos y elimínalo.

## Problemas comunes

| Error | Causa |
|---|---|
| 500.19 | Falta el Hosting Bundle o el módulo URL Rewrite que use tu `web.config` |
| 500.30 / 500.31 | La aplicación falló al iniciar (secretos faltantes) o falta el runtime. Activa temporalmente `stdoutLogEnabled="true"` y revisa el Visor de eventos |
| 502.5 | Proceso fuera de proceso que no arranca; revisa la ruta de `dotnet` |

Más en [troubleshooting.md](../troubleshooting.md#iis).
