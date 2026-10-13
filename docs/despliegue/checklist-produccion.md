# Checklist de producción

- [ ] `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Secretos (`Jwt:SigningKey`, cadenas de conexión, `Seed:AdminPassword`, SMTP) en variables de entorno o almacén de secretos, no en archivos del repositorio.
- [ ] `Jwt:SigningKey` aleatoria de 32 caracteres o más, distinta por entorno.
- [ ] HTTPS con certificado válido; HSTS activo.
- [ ] `AllowedHosts` limitado a tus dominios; `Cors:AllowedOrigins` vacío o restringido.
- [ ] `Hosting:KnownProxies` / `KnownNetworks` configurados si hay proxy o balanceador.
- [ ] `Database:MigrateOnStartup=false` y migraciones ejecutadas con `Project.MigrationService`.
- [ ] Contraseña inicial del administrador fuerte; cambio realizado en el primer acceso.
- [ ] Redis configurado si hay más de una instancia.
- [ ] Respaldos automáticos de la base y prueba de restauración realizada.
- [ ] Logs y trazas exportados (`OTEL_EXPORTER_OTLP_ENDPOINT`) y alerta sobre `/health/ready`.
- [ ] Límites de `RateLimiting` revisados para el tráfico esperado.
- [ ] OpenAPI y Scalar desactivados (`Hosting:EnableApiExplorer=false`, valor por defecto fuera de Development).
- [ ] `Seed:SampleData=false`.
- [ ] Correo real configurado (`Email:Host`) y `Identity:PublicUrl` con el dominio público.
- [ ] `dotnet list package --vulnerable` y `pnpm audit` sin hallazgos altos.
