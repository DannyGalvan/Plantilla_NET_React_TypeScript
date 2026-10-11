# Checklist pre-producción

> Lista marcable. Tildala antes de promover a producción. Vinculada al
> job `publish-smoke` del CI.

## Secretos y configuración

- [ ] Secretos fuera del repo. Variables de entorno o `dotnet user-secrets`.
- [ ] `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] `Database:MigrateOnStartup=false` (las migraciones corren antes del API).
- [ ] HTTPS + HSTS configurado en nginx/IIS/ALB.
- [ ] CORS restringido a los dominios del frontend.
- [ ] `AllowedHosts` con los dominios reales (no `*`).
- [ ] `Jwt:SigningKey` rotado al menos una vez desde el último incidente.
- [ ] `Seed:AdminPassword` fuerte y rotado al primer login.

## Aplicación

- [ ] API en al menos 2 réplicas detrás de un balanceador.
- [ ] Data Protection keys compartidas entre instancias (BD o Redis).
- [ ] Redis obligatorio si hay > 1 instancia (rate limit y permisos).
- [ ] `ForwardedHeaders` con `KnownProxies`/`KnownNetworks` correctos.
- [ ] OutboxProcessor ejecutándose en al menos 2 instancias (idempotente por `ProcessedAt`).
- [ ] Logs y trazas exportados (Serilog a OpenTelemetry → Datadog/Aspire/Loki).
- [ ] Alertas sobre `/health/ready` (HTTP 503 = page).

## Base de datos

- [ ] Backup automático (cron + `pg_dump`/`mysqldump`/`sqlpackage`).
- [ ] Restauración probada en staging al menos una vez al mes.
- [ ] Connection pool dimensionado (`Tenancy:MaxDedicatedPoolSize`).
- [ ] Índices compuestos `(TenantId, …)` donde aplique.

## Red y dominio

- [ ] `Tenancy:Resolvers` en orden coherente con la topología DNS.
- [ ] `Tenancy:RootDomain` apunta al dominio real.
- [ ] Certificados TLS válidos (≤ 30 días restantes).
- [ ] Renovación automática habilitada (`certbot renew`).

## Seguridad

- [ ] Escaneo de vulnerabilidades en verde (`dotnet list package --vulnerable`,
      `pnpm audit --audit-level=high`).
- [ ] Límite de rate limit revisado por plan (`RateLimiting:Global`,
      `RateLimiting:Auth`).
- [ ] Scalar y OpenAPI UI deshabilitados o protegidos en prod
      (`MapOpenApi` solo en `IsDevelopment`).
- [ ] Logs no contienen `ex.Message` al cliente (ver `ResultExtensions.ToHttp`).
- [ ] `ErrorBoundary` no muestra stack al cliente (configurado en `import.meta.env.DEV`).

## Pruebas

- [ ] CI matrix `provider x tenancy` en verde en la última corrida.
- [ ] `Project.IsolationTests` en verde (no cruce de datos entre tenants ni
      unidades).
- [ ] `dotnet ef migrations has-pending-model-changes` en verde.
- [ ] Carga k6 ejecutada en staging (`tests/load/list-products.js` con 200
      VUs durante 2 min, p95 < 300 ms, 0 errores 5xx).

## Documentación

- [ ] CHANGELOG actualizado con el release.
- [ ] [`docs/08-tecnologias.md`](../08-tecnologias.md) refleja la versión de
      cada dependencia.
- [ ] Runbook actualizado con cualquier cambio operativo (nuevo puerto,
      nuevo secreto, nuevo dominio).