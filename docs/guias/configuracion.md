# Configuración

**Para qué sirve este documento:** saber qué se configura al desplegar (pocos valores, en el entorno) y qué se
configura en tiempo de ejecución desde la aplicación (todo lo demás, guardado en la base de datos).
**Tiempo estimado:** 10 minutos.

## Dos niveles

| Nivel | Dónde vive | Cuándo cambia | Ejemplos |
|---|---|---|---|
| Bootstrap | Variables de entorno, user-secrets o `appsettings.json` | Al desplegar | Motor y cadena de conexión, master key, modo de tenancy, módulos |
| Tiempo de ejecución | Base de datos de plataforma (módulo Configuration) | Cuando quieras, desde *Configuración de plataforma* | SMTP, OAuth de Google y Microsoft, JWT, límites, webhooks, auditoría |

Cambiar un valor de tiempo de ejecución **no exige volver a desplegar**:

- Se guarda en la tabla `configuration.Entries`.
- Todas las instancias lo releen cada `Configuration:ReloadSeconds` segundos (10 por defecto). La instancia que guardó lo
  aplica de inmediato.
- Los campos marcados *Requiere reiniciar* se leen al arrancar, como el intervalo de un worker. Aplican al reiniciar
  el proceso, no hace falta un nuevo despliegue.

## Bootstrap

Es lo único que la aplicación necesita antes de poder leer su base de datos. En variables de entorno, `:` se escribe
`__`; por ejemplo, `Database:ConnectionStrings:Default` es `Database__ConnectionStrings__Default`.

| Clave | Obligatoria | Descripción |
|---|---|---|
| `Database:Provider` | Sí | `PostgreSql`, `SqlServer` o `MySql` |
| `Database:ConnectionStrings:Default` | Sí (secreto) | Base de plataforma y de tenants compartidos |
| `Database:ConnectionStrings:Platform` | No | Base separada para los módulos de plataforma |
| `Database:ConnectionStrings:<Modulo>` | No | Base propia de un módulo |
| `Configuration:MasterKey` | Sí en producción (secreto) | Cifra los secretos guardados en la base. Se genera una vez: `openssl rand -base64 32` |
| `Configuration:ReloadSeconds` | No | Cada cuánto se relee la configuración de la base (10) |
| `Tenancy:Mode` | No | `None` (defecto), `SharedDatabase`, `DatabasePerTenant`, `Hybrid` |
| `Tenancy:Resolvers`, `RootDomain`, `HeaderName`, `DefaultTenantDomains` | No | Resolución del tenant por host |
| `Modules:Disabled` / `Modules:Enabled` | No | Módulos que no se cargan / módulos opcionales que sí se cargan |
| `Seed:AdminUserName`, `AdminEmail`, `AdminPassword` | `AdminPassword` fuera de Development | Primer administrador, solo en la primera ejecución |
| `Seed:SampleData` | No | Datos de ejemplo (Development) |
| `Database:MigrateOnStartup` | Prohibido en Production | Migra al iniciar la API |
| `Database:CommandTimeoutSeconds`, `MaxRetryCount` | No | 30 y 5 |
| `Cache:Redis:ConnectionString` | No | Recomendado con varias instancias |
| `Hosting:*` | No | `ServeSpa`, `UseHttpsRedirection`, `EnableApiExplorer`, `KnownProxies`, `KnownNetworks` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | Exportación de trazas, métricas y logs |

### La master key

Todo lo sensible que vive en la base se cifra con AES-256-GCM usando `Configuration:MasterKey`: los secretos de
configuración y el anillo de llaves de Data Protection, que a su vez protege los tokens OAuth y los secretos de
webhooks. Así un respaldo filtrado de la base no revela nada.

- Guárdala fuera de la base: variable de entorno, almacén de secretos o user-secrets en desarrollo. Respáldala en tu
  bóveda.
- **No la cambies una vez en uso.** Con otra llave, los secretos guardados no se pueden leer y hay que volver a
  escribirlos.
- `scripts/setup` la genera solo si no existe.
- Sin master key la aplicación arranca, pero no puede guardar secretos ni generar la llave JWT; la pantalla lo avisa.

## Tiempo de ejecución

En *Configuración de plataforma* (permiso `configuration.platform.manage`, solo el administrador de plataforma). Cada
campo muestra su valor por defecto, su origen (*Base de datos*, *Variable de entorno*, *appsettings* o *Por defecto*) y
si requiere reiniciar.

| Sección | Qué configura |
|---|---|
| `Jwt` | Emisor, audiencia, duración de tokens y llave de firma. La llave se genera en el primer arranque; cambiarla cierra todas las sesiones |
| `Email` | Servidor SMTP. Sin `Host`, los correos se escriben en el log |
| `Identity` | URL pública para enlaces de correo, vigencia del restablecimiento de contraseña |
| `Files` | Almacenamiento de la plataforma, clientes OAuth de Google y Microsoft, endpoints personalizados |
| `Webhooks` | Tiempo de espera y redes privadas permitidas |
| `Audit` | Activación, exclusiones y retención del historial |
| `Notifications` | Retención de notificaciones leídas |
| `Jobs`, `Outbox` | Intervalos de los procesos en segundo plano (requieren reiniciar) |
| `RateLimiting` | Límites por minuto y de inicio de sesión |
| `Cors` | Orígenes permitidos cuando la SPA vive en otro dominio |
| `Pagination` | Tamaño máximo de página |
| `Features` | Valores por defecto de los feature flags (cada tenant puede cambiarlos) |
| `Secrets` | Secretos referenciados por nombre, por ejemplo la cadena de conexión de un tenant dedicado |

Las reglas:

- **Secretos.** Se guardan cifrados y nunca se devuelven: la pantalla solo dice si están configurados. Para cambiar uno
  escribe el valor nuevo.
- **Prioridad.** Una variable de entorno gana sobre la base. Úsala solo como override de emergencia; la pantalla marca
  esos campos.
- **Volver al valor por defecto.** *Usar valor por defecto* borra el valor guardado.
- **Historial.** Si el módulo Audit está activo, cada cambio queda registrado. Los secretos aparecen enmascarados.

Los settings de negocio por tenant, unidad y usuario (marca, localización, impuestos, archivos) son otra cosa: se
editan en *Configuración* de cada organización. Ver [Organización](organizacion.md).

## Agregar una sección desde un módulo

```csharp
public sealed class PaymentsOptions
{
    public string? Provider { get; set; }

    [Secret]
    public string? ApiKey { get; set; }

    [RequiresRestart]
    public int PollingSeconds { get; set; } = 30;
}

services.AddOptions<PaymentsOptions>().Bind(configuration.GetSection("Payments"));
services.AddConfigurableSection<PaymentsOptions>("Payments", "payments", "Pasarela de pagos.");
```

- **Lectura en el consumidor.** Un singleton usa `IOptionsMonitor<PaymentsOptions>.CurrentValue`; un servicio scoped usa
  `IOptionsSnapshot<PaymentsOptions>`. Así el cambio aplica sin reiniciar.
- **Interfaz.** La pantalla se genera sola desde la clase: textos, números, booleanos, listas (`string[]`), mapas
  (`Dictionary<string, string>`) y clases anidadas.
- **Validaciones.** Se aplican los atributos de validación de cada propiedad, como `[Range]` o `[EmailAddress]`.

## Cómo verificar que funcionó

- En *Configuración de plataforma*, la sección `Jwt` muestra la llave de firma como *Configurado (oculto)* con origen
  *Base de datos*.
- Cambia `Pagination / MaxPageSize`: el siguiente listado ya respeta el nuevo máximo, sin reiniciar.

## Problemas comunes

Ver [troubleshooting.md](../troubleshooting.md).
