# Security

> Las decisiones del endurecimiento OWASP heredado + los añadidos de Fase 1-4.

## Backend

- **JWT** ≥ 32 bytes UTF-8 (`Jwt:SigningKey`). Validación de issuer +
  audience + lifetime + clock skew 30s. HS256 o RS256 (configurable).
- **Refresh tokens**: hash SHA-256, rotación en cada refresh, revocación
  de cadena en reuso. Cookie `HttpOnly` + `Secure` (siempre fuera de
  Development) + `SameSite=Strict` + path `/api/v1/identity/auth`. CSRF
  double-submit.
- **Lockout**: 5 intentos / 30 min (configurable).
- **Passwords**: BCrypt con work factor 12. Política: 8+ chars, upper/lower,
  digit, no alfanumérico. Política ampliable por settings.
- **Permisos**: `{module}.{resource}.{action}`. Cache `perm:{tid}:{userId}:{pv}`.
  Invalidación por `RolePermissionsChanged` (incrementa `pv`).
- **Fail-closed**: cada endpoint lleva `RequirePermission` o
  `AllowAnonymous`. `ArchitectureTests` lo verifica.
- **Errores**: siempre `ProblemDetails` con `code`, `traceId`, `errors`.
  Sin `ex.Message` al cliente fuera de dev.

## Frontend

- Access token **solo en memoria** (no `localStorage`).
- Refresh token en cookie `HttpOnly` (backend).
- CSP `<meta>` de respaldo en `index.html`.
- `<a target="_blank">` siempre con `rel="noopener noreferrer"`.
- `ErrorBoundary` y error pages sin stack ni email en producción; traceId
  solo en dev.
- Tokens, credenciales y datos sensibles **nunca** en `localStorage`.

## Verificación

- `dotnet ef migrations has-pending-model-changes` en CI.
- `ArchitectureTests` con `RequirePermissionConvention` cuando se enchufe
  el fail-closed al `MapCrud`.
- `Project.IsolationTests` (Fase 5): ningún endpoint CRUD ni query Dapper
  devuelve, cuenta, actualiza o borra filas de otro tenant ni de otra
  unidad no accesible.
- CI: `dotnet list Project.sln package --vulnerable --include-transitive`
  bloquea en PR.