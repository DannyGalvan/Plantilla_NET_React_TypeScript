# Configuración inicial de un proyecto

**Para qué sirve este documento:** checklist del primer día de un proyecto creado con la plantilla.
**Tiempo estimado:** 30 minutos.

1. **Elige la topología** con la tabla de [00-objetivo-y-alcance.md](00-objetivo-y-alcance.md) y fija `Tenancy:Mode`.
   Si no necesitas empresas ni marcas, quita `new OrganizationModule()` de `src/Host/Project.Composition/AppModules.cs`
   o agrégalo a `Modules:Disabled`.
2. **Elige el motor** con `Database:Provider` (`PostgreSql`, `SqlServer` o `MySql`) y la cadena
   `Database:ConnectionStrings:Default`.
3. **Genera los dos secretos de arranque** y guárdalos fuera del repositorio. `scripts/setup` lo hace por ti.

   ```bash
   dotnet user-secrets set "Configuration:MasterKey" "$(openssl rand -base64 32)" --project src/Host/Project.Api
   dotnet user-secrets set "Seed:AdminPassword" "<contraseña-fuerte>" --project src/Host/Project.Api
   ```

   ```powershell
   dotnet user-secrets set "Configuration:MasterKey" ([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))) --project src/Host/Project.Api
   dotnet user-secrets set "Seed:AdminPassword" "<contraseña-fuerte>" --project src/Host/Project.Api
   ```

   En servidores usa variables de entorno (`:` se escribe `__`): `Configuration__MasterKey` y
   `Database__ConnectionStrings__Default`. La llave JWT no se crea a mano: se genera en el primer arranque y se guarda
   cifrada.
4. **Hosts y dominios:** `AllowedHosts` y `Tenancy:RootDomain` para subdominios.
5. **Redis** (`Cache:Redis:ConnectionString`) si vas a ejecutar más de una instancia.
6. **Observabilidad:** `OTEL_EXPORTER_OTLP_ENDPOINT` para enviar trazas, métricas y logs.
7. **Migraciones y semillas:** `dotnet run --project src/Host/Project.MigrationService` (o automático en Development).
8. **Primer acceso:** inicia sesión con `admin` y cambia la contraseña.
9. **Configura en la aplicación**, sin volver a desplegar:
   - En *Configuración de plataforma*: correo SMTP (`Email`), URL pública (`Identity`), almacenamiento y OAuth de Google
     o Microsoft (`Files`), orígenes CORS si la SPA vive en otro dominio, y límites de peticiones.
   - En *Configuración*: marca (nombre, logo, colores) y localización (idioma, moneda, zona horaria), por tenant o por
     unidad.
10. **Crea roles y usuarios.**
11. **Renombra** la solución y los espacios de nombres si lo deseas (buscar y reemplazar `Project.`).

Qué va en el entorno y qué se configura desde la aplicación: [guias/configuracion.md](guias/configuracion.md).
