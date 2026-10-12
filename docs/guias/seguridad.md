# Seguridad

## Autenticación

- **Token de acceso:** JWT HS256 de 15 minutos con `sub`, `name`, `tid` (tenant), `ou` (unidad), `pv` (versión de
  permisos) y roles. No contiene permisos. Se guarda solo en memoria en la SPA.
- **Refresh token:** valor aleatorio de 256 bits en una cookie `HttpOnly`, `SameSite=Strict`, `Secure` fuera de
  Development y limitada a `/api/v1/identity/auth`. En la base solo se guarda su hash SHA-256.
- **Rotación y reuso:** cada refresh revoca el token usado y emite otro de la misma familia. Presentar un token ya
  rotado revoca toda la familia (posible robo).
- **CSRF:** `/refresh` y `/logout` exigen la cabecera `X-XSRF-TOKEN` igual a la cookie `XSRF-TOKEN` (doble envío).
- **Contraseñas:** BCrypt (factor 12, rehash automático), política configurable (`Settings:security`), historial de las
  últimas N, bloqueo tras intentos fallidos, restablecimiento por correo con token de un solo uso. El primer
  administrador debe cambiar su contraseña.
- **Enumeración:** el login responde igual y con el mismo costo cuando el usuario no existe; `forgot-password` siempre
  responde 204.

## Autorización

- Permisos `{modulo}.{recurso}.{accion}` declarados por cada módulo y sincronizados por el migrador.
- Se evalúan en el servidor con caché híbrida por usuario, tenant y unidad; cambiar los permisos de un rol invalida la
  caché de sus usuarios, sin volver a iniciar sesión.
- **Fail-closed:** si un endpoint de `/api` no declara `RequirePermission`, `RequireAuthenticatedUser` o
  `AllowAnonymous`, la API no arranca.
- Los permisos de administración de tenants solo pueden asignarse al rol `SuperAdmin` de plataforma.

## Datos

- Aislamiento por tenant y unidad con filtros globales y verificación en el guardado (ver [multitenancy](multitenancy.md)).
- Filtros de consulta por allowlist: campos no declarados devuelven 400; los valores siempre son parámetros SQL.
- Los DTO de entrada no exponen `Id`, `TenantId`, auditoría ni campos de seguridad (sin asignación masiva).
- Recursos de otro tenant, unidad o dueño responden 404 (no revelan existencia).

## HTTP

- Cabeceras: CSP sin scripts inline, `X-Frame-Options: DENY`, `X-Content-Type-Options`, `Referrer-Policy`,
  `Permissions-Policy`, `Cross-Origin-Opener-Policy`, HSTS fuera de Development.
- Rate limiting global por tenant y usuario o IP, y política estricta en login y recuperación.
- Errores como ProblemDetails con `traceId`; nunca se devuelven mensajes ni trazas de excepciones en producción.
- `ForwardedHeaders` solo confía en `Hosting:KnownProxies` y `KnownNetworks`.
- OpenAPI y Scalar solo en Development (o `Hosting:EnableApiExplorer=true`).

## Secretos

Nada sensible en el repositorio: user-secrets en desarrollo, variables de entorno o almacén de secretos en servidores.
Las cadenas de tenants dedicados se guardan como referencia y se resuelven con `ISecretStore`.
