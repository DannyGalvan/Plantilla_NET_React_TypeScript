# Archivos y almacenamiento

**Para qué sirve este documento:** configurar dónde se guardan los archivos de la plataforma y permitir que cada tenant
conecte su propio Google Drive, OneDrive, bucket S3 o contenedor de Azure Blob.
**Requisitos previos:** módulo `Files` activo (por defecto) y `Configuration:MasterKey` configurada. Ver
[Configuración](configuracion.md).
**Tiempo estimado:** 30 minutos con OAuth; 5 minutos sin él.

## Modelo

| Nivel | Quién lo configura | Proveedores |
|---|---|---|
| Plataforma (`Files / Default`) | Administrador de plataforma (*Configuración de plataforma*) | `local`, `s3`, `azure-blob`, `google-drive` (cuenta de servicio), `onedrive` (app de Entra) |
| Tenant (*Almacenamiento*) | Administrador del tenant (`files.storage.manage`) | `google-drive`, `onedrive`, `s3`, `azure-blob` |

Detalles del modelo:

- Los archivos nuevos van a la conexión marcada como predeterminada del tenant, o al almacenamiento de la plataforma.
- Cada archivo recuerda dónde se guardó, así que cambiar la predeterminada no mueve los archivos existentes.
- Las credenciales se cifran con Data Protection. Las llaves se comparten entre instancias a través de la base de
  plataforma.
- La API nunca devuelve las credenciales.

## Almacenamiento de la plataforma

Se configura en *Configuración de plataforma*, sección `Files`, sin volver a desplegar:

- `Default / Provider`: `local`, `s3`, `azure-blob`, `google-drive` u `onedrive`.
- `Default / Settings`: valores no secretos, como `rootPath`, `bucket`, `region`, `prefix`, `container`, `folderId` o
  `driveId`.
- `Default / Credentials`: llaves del proveedor. Se guardan cifradas con la master key.

Ejemplo para S3: `Provider = s3`; en `Settings`, `bucket = <TU_BUCKET>` y `region = us-east-1`; en `Credentials`,
`accessKeyId` y `secretAccessKey`.

En un bucket compartido, cada objeto lleva el prefijo del tenant: `<tenantId>/<año>/<mes>/<id>.ext`.

## Conectar Google Drive (OAuth)

1. En Google Cloud Console crea un proyecto y habilita **Google Drive API**.
2. Configura la pantalla de consentimiento con el alcance `.../auth/drive.file`. La aplicación solo ve los archivos que
   ella misma crea.
3. Crea credenciales **OAuth client ID** de tipo *Web application*. La URI de redirección autorizada es
   `https://<tu-dominio>/api/v1/files/oauth/callback`.
4. En *Configuración de plataforma*, sección `Files`, completa `OAuth / Google / ClientId`,
   `OAuth / Google / ClientSecret` (se guarda cifrado) y `OAuth / RedirectUri`. Se aplica al instante, sin reiniciar.

5. El administrador del tenant entra a *Almacenamiento*, pulsa **Conectar Google Drive** y acepta. Se guarda el refresh
   token cifrado. Si solo se pasó una URI de redirección, aplica a todos los tenants, porque el `state` cifrado lleva el
   tenant y la página de regreso.

Alternativa sin consentimiento: una cuenta de servicio con acceso a una unidad compartida. Crea la conexión por API
(`POST /api/v1/files/storage/connections`) con `provider: "google-drive"`, las credenciales `client_email` y
`private_key` del JSON de la cuenta y, en `settings`, `folderId`.

## Conectar OneDrive o SharePoint (OAuth)

1. En Microsoft Entra registra una aplicación con cuentas *de cualquier directorio y personales*, o solo de tu
   organización.
2. Agrega la URI de redirección web `https://<tu-dominio>/api/v1/files/oauth/callback`.
3. Concede los permisos delegados `Files.ReadWrite`, `User.Read` y `offline_access`, y crea un secreto de cliente.
4. En *Configuración de plataforma*, sección `Files`, completa `OAuth / Microsoft / ClientId`,
   `OAuth / Microsoft / ClientSecret` y, si aplica, `OAuth / Microsoft / Tenant` (`common` por defecto).
5. El tenant pulsa **Conectar OneDrive**. Microsoft rota el refresh token en cada uso y la plataforma guarda el nuevo
   automáticamente.

Para una biblioteca de SharePoint sin usuario, crea la conexión con `tenant_id`, `client_id` y `client_secret` de una
aplicación con permiso de aplicación `Files.ReadWrite.All`, y el setting `driveId`. Los archivos de más de 4 MB se
suben por sesión, en partes de 10 MB.

## Seguridad

- **Endpoints personalizados.** Un tenant no puede apuntar S3 o Azure a direcciones arbitrarias (`serviceUrl`,
  `BlobEndpoint`), salvo con `Files / AllowCustomEndpoints` activado. Actívalo solo si usas MinIO o Azurite en desarrollo.
  Evita SSRF.
- **Archivos permitidos.** Las extensiones y el tamaño máximo son la sección de settings `files`, editable por tenant y
  por unidad. Las descargas usan `Content-Disposition: attachment`, salvo imágenes, PDF y texto con `?inline=true`.
- **Retención.** Los archivos borrados se pueden restaurar durante `files.PurgeDeletedAfterDays` días (30 por defecto).
  Después, la tarea `files.purge-deleted` elimina el contenido del proveedor.

## Cómo verificar que funcionó

- *Archivos* > **Subir archivo**: el archivo aparece en la lista con el proveedor esperado.
- *Almacenamiento*: la conexión aparece como **Predeterminado** con la cuenta conectada.

## Problemas comunes

| Síntoma | Causa |
|---|---|
| El botón **Conectar Google Drive** está deshabilitado | Faltan `OAuth / Google / ClientId` o `ClientSecret` en la sección `Files` |
| `redirect_uri_mismatch` en Google o Microsoft | La URI registrada no es igual a `OAuth / RedirectUri` |
| `files.storage_disconnected` al descargar | La conexión que guardó el archivo fue eliminada o cambió el proveedor de plataforma |
| `The credentials ... cannot be decrypted` | Se perdió el anillo de llaves de Data Protection (tabla `tenancy.DataProtectionKeys`) |
