# Checklist de producción

- [ ] `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] `Configuration:MasterKey`, cadenas de conexión y `Seed:AdminPassword` en variables de entorno o almacén de secretos, no en archivos del repositorio.
- [ ] `Configuration:MasterKey` distinta por entorno y respaldada en tu bóveda (sin ella no se leen los secretos guardados).
- [ ] La sección `Jwt` de *Configuración de plataforma* muestra la llave de firma generada, con origen *Base de datos*.
- [ ] HTTPS con certificado válido; HSTS activo.
- [ ] `AllowedHosts` limitado a tus dominios; `Cors / AllowedOrigins` vacío o restringido.
- [ ] `Hosting:KnownProxies` / `KnownNetworks` configurados si hay proxy o balanceador.
- [ ] `Database:MigrateOnStartup=false` y migraciones ejecutadas con `Project.MigrationService`.
- [ ] Contraseña inicial del administrador fuerte; cambio realizado en el primer acceso.
- [ ] Redis configurado si hay más de una instancia.
- [ ] Respaldos automáticos de la base y prueba de restauración realizada.
- [ ] Logs y trazas exportados (`OTEL_EXPORTER_OTLP_ENDPOINT`) y alerta sobre `/health/ready`.
- [ ] Límites de `RateLimiting` revisados para el tráfico esperado (*Configuración de plataforma*).
- [ ] OpenAPI y Scalar desactivados (`Hosting:EnableApiExplorer=false`, valor por defecto fuera de Development).
- [ ] `Seed:SampleData=false`.
- [ ] Correo real configurado (`Email`) e `Identity / PublicUrl` con el dominio público (*Configuración de plataforma*).
- [ ] `Webhooks / AllowPrivateNetworks` y `Files / AllowCustomEndpoints` en `false`.
- [ ] `dotnet list package --vulnerable` y `pnpm audit` sin hallazgos altos.
