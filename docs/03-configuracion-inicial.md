# Configuración inicial de un proyecto

**Para qué sirve este documento:** checklist del primer día de un proyecto creado con la plantilla.
**Tiempo estimado:** 30 minutos.

1. **Elige la topología** con la tabla de [00-objetivo-y-alcance.md](00-objetivo-y-alcance.md) y fija `Tenancy:Mode`.
   Si no necesitas empresas ni marcas, quita `new OrganizationModule()` de `src/Host/Project.Composition/AppModules.cs`
   o agrégalo a `Modules:Disabled`.
2. **Elige el motor** con `Database:Provider` (`PostgreSql`, `SqlServer` o `MySql`) y la cadena
   `Database:ConnectionStrings:Default`.
3. **Genera los secretos** y guárdalos fuera del repositorio:

   ```bash
   dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/Host/Project.Api
   dotnet user-secrets set "Seed:AdminPassword" "<contraseña-fuerte>" --project src/Host/Project.Api
   ```

   En servidores usa variables de entorno (`:` se escribe `__`): `Jwt__SigningKey`, `Database__ConnectionStrings__Default`.
4. **Hosts y dominios:** `AllowedHosts`, `Tenancy:RootDomain` para subdominios y `Cors:AllowedOrigins` solo si la SPA
   se sirve desde otro dominio.
5. **Marca:** nombre, logo, preset y color en la sección `Settings:branding` de `appsettings.json` o desde la pantalla
   *Configuración* (por tenant o por unidad).
6. **Localización:** `Settings:localization` (idioma por defecto, moneda, zona horaria).
7. **Redis** (`Cache:Redis:ConnectionString`) si vas a ejecutar más de una instancia.
8. **Observabilidad:** `OTEL_EXPORTER_OTLP_ENDPOINT` para enviar trazas, métricas y logs.
9. **Correo:** `Email:Host`, `Email:Port`, `Email:From` (sin `Email:Host` los correos se escriben en el log).
10. **Migraciones y semillas:** `dotnet run --project src/Host/Project.MigrationService` (o automático en Development).
11. **Primer acceso:** inicia sesión con `admin`, cambia la contraseña, crea roles y usuarios.
12. **Renombra** la solución y los espacios de nombres si lo deseas (buscar y reemplazar `Project.`).

La referencia completa de opciones está en [guias/configuracion.md](guias/configuracion.md).
