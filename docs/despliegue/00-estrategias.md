# Estrategias de despliegue

**Para qué sirve este documento:** elegir dónde y cómo publicar la aplicación.

## Comparativa

| Destino | Complejidad | Escalado | Cuándo conviene | Guía |
|---|---|---|---|---|
| Docker / Compose | Baja | Réplicas detrás de un proxy | Cualquier servidor con Docker, nubes, VPS | [docker.md](docker.md) |
| Linux nativo | Media | Varias instancias con nginx | Servidores Linux sin contenedores | [linux-systemd-nginx.md](linux-systemd-nginx.md) |
| IIS | Media | Web farm | Infraestructura Windows existente | [iis-windows-server.md](iis-windows-server.md) |

Cualquier plataforma que ejecute contenedores (Kubernetes, Azure Container Apps, AWS ECS) usa las mismas imágenes de
`deploy/docker` con las sondas `/health/live` y `/health/ready`.

## Topologías

1. **SPA servida por la API** (por defecto): un solo despliegue, sin CORS. La SPA compilada se publica en `wwwroot`.
2. **SPA separada** (CDN o nginx propio): `Hosting:ServeSpa=false`, `Cors:AllowedOrigins` con el dominio de la SPA. La
   cookie de refresh es `SameSite=Strict`, por lo que la SPA y la API deben compartir el sitio (por ejemplo
   `app.miempresa.com` y `api.miempresa.com`).

## Orden de despliegue

1. Respaldo de la base de datos.
2. Migraciones con `Project.MigrationService` ([migraciones en producción](migraciones-en-produccion.md)).
3. Despliegue de la nueva versión de la API.
4. Verificación: `GET /health/ready` responde 200 e inicio de sesión de prueba.
5. Si falla: volver a la versión anterior de la API. Las migraciones deben ser compatibles hacia atrás (patrón
   expandir y luego contraer) para que la versión anterior siga funcionando.

Antes de salir a producción revisa el [checklist](checklist-produccion.md).
