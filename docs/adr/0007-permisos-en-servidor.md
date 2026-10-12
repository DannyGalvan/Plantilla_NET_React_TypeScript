# ADR 0007 — Permisos resueltos en el servidor con caché híbrida

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** el token con todos los permisos crece con cada módulo y obliga a volver a iniciar sesión cuando cambian los permisos de un rol.
- **Decisión:** el JWT solo lleva usuario, tenant, unidad, roles y `pv` (versión de permisos). `PermissionService` calcula los permisos efectivos (roles del tenant más roles de la unidad activa y sus ancestros) y los guarda en `HybridCache` con etiquetas `perm-user:{id}` y `perm-all`.
- **Consecuencias:**
  - Cambiar los permisos de un rol invalida la caché de sus usuarios: efecto inmediato sin re-login.
  - Una consulta a caché por petición autorizada; con varias instancias se recomienda Redis.
  - El frontend obtiene los permisos de `/api/v1/me`.
- **Alternativas descartadas:** permisos como claims (token grande, cambios tardíos), consulta a la base en cada petición (costo).
