# 00 · Estrategias de despliegue

## Comparativa

| Estrategia | Complejidad | Escala | Costes | Cuándo conviene |
|---|---|---|---|---|
| Docker único (`docker compose up`) | baja | vertical (1 host) | bajo | demo, MVP, validación |
| Docker Swarm | baja-media | horizontal (≤ 50 nodos) | bajo | equipos sin experiencia k8s |
| IIS + Windows Server | media | vertical + ARR | medio | empresas con infraestructura Windows |
| Linux + systemd + nginx | baja-media | vertical | bajo | apps pequeñas en VMs |
| Kubernetes (k8s) | alta | horizontal (sin techo práctico) | medio-alto | > 3 instancias, multi-región, SRE |
| App Service / Container Apps | baja | auto | medio | "no quiero administrar VMs" |
| Aspire (dev) | muy baja | N/A | bajo | desarrollo local |

## Topologías

### 1. SPA servida por el propio API (`Hosting:ServeSpa=true`, default)

- Un solo artefacto. Sin CORS. El reverse proxy redirige `/` a `wwwroot/`.
- Más simple. Menos coste.

### 2. SPA en CDN / nginx separado

- API en un dominio, frontend en otro. Cookies SameSite=None + Secure.
- Más complejo. Requiere configurar `Hosting:ServeSpa=false` y CORS.
- Útil si el frontend se cachea agresivamente (Cloudflare, CloudFront) o se sirve desde S3+CloudFront.

## Orden de despliegue universal

1. Backup de la BD (si existe).
2. Correr `MigrationService` (o `efbundle`).
3. Esperar `/health/ready` del API.
4. Rolling restart de las instancias de API.
5. Smoke test del endpoint raíz.
6. Si rollback: reverse migration + redeploy versión anterior.

## Comprobación previa

[`docs/despliegue/checklist-produccion.md`](checklist-produccion.md) tiene la lista marcable de pre-producción. Tildala antes de promover a producción.