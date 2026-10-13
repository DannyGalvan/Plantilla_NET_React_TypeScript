# Glosario

| Término | Definición |
|---|---|
| Módulo | Unidad funcional independiente (Identity, Catalog...) con sus cuatro proyectos y su carpeta en el frontend |
| BuildingBlocks | Proyectos compartidos que forman la plataforma (`SharedKernel`, `Application`, `Infrastructure`, `Web`) |
| Contracts | Proyecto público de un módulo: permisos, eventos e interfaces que otros módulos pueden usar |
| Tenant | Frontera de aislamiento: cliente con sus datos, usuarios, configuración y opcionalmente su base |
| Unidad organizativa | Nodo de la jerarquía de negocio dentro de un tenant (empresa, país, marca, sucursal) |
| DataScope | `Platform` (datos globales en la base de plataforma) o `Tenant` (datos de negocio en la base del tenant) |
| Base de plataforma | Base con tenants, usuarios, roles y settings |
| Tenant dedicado | Tenant con base de datos propia (`Isolation=Dedicated`) |
| Permiso | Clave `{modulo}.{recurso}.{accion}` evaluada en el servidor |
| Fail-closed | Lo no declarado se rechaza: un endpoint sin declaración de seguridad impide el arranque |
| Settings en cascada | Configuración resuelta de lo general (sistema) a lo específico (usuario) |
| Feature flag | Interruptor por tenant que activa funciones o módulos |
| Outbox | Tabla donde se guardan eventos en la misma transacción para publicarlos después de forma confiable |
| Query policy | Allowlist de campos filtrables y ordenables de un recurso |
| Merge Patch | Actualización parcial (RFC 7396): solo cambian las propiedades enviadas |
| ProblemDetails | Formato estándar de errores HTTP (RFC 9457) |
| Refresh token rotativo | Token de larga duración que se reemplaza en cada uso; reutilizar uno viejo revoca la sesión |
| Token de diseño | Variable CSS semántica (`--primary`) que define el aspecto de la UI |
| Adjunto | Archivo ligado a un registro (`entityType` + `entityId`) que hereda sus permisos y su alcance |
| Conexión de almacenamiento | Cuenta de Google Drive, OneDrive, S3 o Azure Blob conectada por un tenant |
| Plantilla de notificación | Texto por idioma con `{{placeholders}}` declarado por un módulo y editable por el tenant |
| Tarea programada | `IRecurringJob` con cron que corre por tenant en su zona horaria |
| Secuencia | Numeración de documentos con formato (`FAC-{yyyy}-{0000}`) por tenant y unidad |
| Flujo de aprobación | Pasos con aprobador por permiso, rol o usuario; publica `ApprovalCompletedEvent` al terminar |
| Evento público | Evento declarado con `AddWebhookEvent` que los tenants reciben por webhook |
| Idempotencia | Procesar dos veces el mismo mensaje no cambia el resultado; ver `IEventContext` |
