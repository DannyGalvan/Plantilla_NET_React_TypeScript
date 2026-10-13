# Capacidades transversales

**Para qué sirve este documento:** conocer los servicios que cualquier módulo de negocio puede usar sin escribir
infraestructura: archivos, notificaciones, historial de cambios, tareas programadas, numeración, dinero, aprobaciones,
importación y exportación, y webhooks.
**Requisitos previos:** [07 Agregar un módulo](../07-agregar-un-modulo.md).
**Tiempo estimado:** 20 minutos.

## Cómo están construidas

Todas siguen el mismo patrón, que mantiene la plantilla modular:

| Pieza | Dónde vive | Para qué |
|---|---|---|
| Abstracción (`IFileService`, `INotificationSender`...) | `Project.Application` | Lo único que conoce un módulo de negocio |
| Registro declarativo (`AddAttachmentTarget`, `AddRecurringJob`...) | `Project.Application` | El módulo declara lo que ofrece, sin depender del módulo que lo usa |
| Implementación por defecto (NoOp) | `Project.Infrastructure` | La aplicación funciona aunque el módulo esté desactivado |
| Módulo propio (`Files`, `Notifications`...) | `src/Modules/<Nombre>` | Implementación real, tablas, endpoints y página de administración |

Consecuencias:

- **Desactivar una capacidad** es configuración: `Modules:Disabled: ["Webhooks"]`. El front oculta sus páginas solo.
- **Ningún módulo de negocio depende de otro módulo.** Depende de `Project.Application` y, si hace falta, de los
  `Contracts` de otro módulo. Las pruebas de arquitectura lo verifican.
- **Multi-tenant por construcción.** Todas las tablas son `IMultiTenant` y se filtran por tenant. Los jobs, el outbox,
  las notificaciones y los webhooks corren dentro del tenant que los originó.

## Resumen

| Capacidad | Módulo | Uso desde un módulo de negocio | Administración |
|---|---|---|---|
| Archivos y adjuntos | `Files` | `AddAttachmentTarget<T>`, `IFileService` | *Archivos*, *Almacenamiento* |
| Notificaciones | `Notifications` | `AddNotificationTemplate`, `INotificationSender` | Campana, *Plantillas de notificación* |
| Historial de cambios | `Audit` | Automático para toda entidad `IAuditable` | *Auditoría*, `HistoryPanel` |
| Tareas programadas | `Jobs` | `AddRecurringJob<T>` | *Tareas programadas* |
| Numeración | `Sequences` | `AddSequence`, `ISequenceGenerator` | *Numeración* |
| Dinero | SharedKernel | `Money` | No aplica |
| Aprobaciones | `Workflows` | `AddWorkflow`, `IApprovalService`, `ApprovalCompletedEvent` | *Aprobaciones*, *Flujos de aprobación* |
| Importar y exportar | Motor CRUD | Automático con `MapCrud` | Botones *Exportar* e *Importar* |
| Webhooks | `Webhooks` | `AddWebhookEvent<T>` | *Webhooks* |

## Archivos y adjuntos

Declara qué entidades aceptan adjuntos y con qué permisos. Los adjuntos heredan los permisos y el alcance (tenant y
unidad) del registro: un usuario que no ve el producto tampoco ve sus archivos.

```csharp
services.AddAttachmentTarget<Employee>("hr.employee", HrPermissions.Employees.Read, HrPermissions.Employees.Update);
```

En el front:

```tsx
<Attachments entityType="hr.employee" entityId={employee.id} canWrite={canUpdate} />
```

Para guardar un archivo generado (un PDF de nómina, por ejemplo) usa `IFileService.UploadAsync`. Cada tenant elige
dónde se guardan sus archivos: disco o bucket de la plataforma, su Google Drive, OneDrive, S3 o Azure Blob. Ver
[Archivos y almacenamiento](archivos-y-almacenamiento.md).

## Notificaciones

Declara la plantilla en cada idioma y envía. La entrada aparece en la campana del usuario y, con `Email: true`, se envía
un correo por el outbox, con reintentos. Los valores se escapan, así que no hay inyección de HTML.

```csharp
services.AddNotificationTemplate(new("hr.leave.approved", "hr", new Dictionary<string, LocalizedTemplate>
{
    ["es"] = new("Vacaciones aprobadas", "{{approver}} aprobó tus vacaciones del {{from}} al {{to}}."),
    ["en"] = new("Leave approved", "{{approver}} approved your leave from {{from}} to {{to}}."),
}, ["approver", "from", "to"]));

await notifications.SendAsync(new Notify("hr.leave.approved", [employeeUserId],
    new Dictionary<string, string> { ["approver"] = name, ["from"] = from, ["to"] = to }, Link: "/hr/leaves", Email: true), ct);
```

Cada tenant puede cambiar el texto en *Plantillas de notificación*. Envía desde un `IDomainEventHandler` para que la
notificación no se pierda si la transacción de negocio falla.

## Historial de cambios

Sin código. Toda entidad `IAuditable` (por ejemplo, las que heredan de `AuditableAggregateRoot`) registra quién la creó,
cambió, borró o restauró, con valores anteriores y nuevos. El registro viaja por el outbox del módulo, en la misma
transacción.

- `[NotAudited]` en la clase excluye una entidad.
- `[AuditIgnore]` en una propiedad la excluye.
- Las propiedades cuyo nombre contiene `Password`, `Secret`, `Token`, `Credential` o `Hash` nunca se registran.

En el front, `<HistoryPanel entityType="hr.employee" entityId={id} />` muestra el historial del registro a quien tenga
`audit.history.view`. El tipo de entidad es `<esquema>.<entidad>` en camelCase.

## Tareas programadas

```csharp
public sealed class ClosePayrollJob(IRepository<Payroll, Guid> payrolls, INotificationSender notifications) : IRecurringJob
{
    public async Task ExecuteAsync(CancellationToken ct) { /* ya está dentro del tenant */ }
}

services.AddRecurringJob<ClosePayrollJob>("payroll.close", "0 2 1 * *", description: "Cierra la nómina del mes.");
```

- El cron tiene cinco campos y se evalúa en la zona horaria del tenant (`localization.TimeZone`).
- Con varias instancias, un lease en la base garantiza una sola ejecución.
- Cada tenant puede pausar la tarea, cambiar su horario o ejecutarla ahora, y ver el historial.
- Con `JobScope.Platform` se ejecuta una vez para toda la plataforma.

Tareas incluidas: `files.purge-deleted`, `audit.retention` y `notifications.cleanup`.

## Numeración y dinero

```csharp
services.AddSequence(new("sales.invoice", "FAC-{yyyy}-{00000}", SequenceReset.Yearly, PerOrganizationUnit: true));

var number = await sequences.NextAsync("sales.invoice", ct); // FAC-2026-00042
```

- Los números son únicos y crecientes por tenant, y por unidad si se pide, aun con muchas peticiones simultáneas.
- Tokens: `{yyyy}`, `{yy}`, `{MM}`, `{dd}` y el contador con ceros.
- El tenant puede cambiar el formato en *Numeración*.

Un número se consume aunque la transacción de negocio falle después. Para numeración fiscal estricta, pide el número
justo antes de guardar.

`Money` es un value object con moneda ISO 4217. No permite sumar monedas distintas, redondea de forma comercial y
reparte montos sin perder centavos:

```csharp
var total = new Money(100m, "GTQ") * 1.12m;
var cuotas = total.Round().Allocate(3); // 37.34, 37.33, 37.33
```

Para mapearlo en EF Core usa `builder.ComplexProperty(x => x.Total)`.

## Aprobaciones

```csharp
services.AddWorkflow(new("purchasing.order", "purchasing", "Orden de compra",
[
    new("Jefe del área", ApproverKind.Permission, "purchasing.orders.approve"),
    new("Finanzas", ApproverKind.Role, "Finance", MinAmount: 10_000),
]));

await approvals.SubmitAsync(new ApprovalSubmission("purchasing.order", "purchasing.order", order.Id.ToString(),
    $"Orden {order.Number}", currentUserId, order.Total.Amount, Link: $"/purchasing/orders/{order.Id}"), ct);
```

- El aprobador de cada paso puede ser un permiso, un rol o un usuario.
- Un paso con `MinAmount` solo aplica desde ese monto.
- Al enviar se guarda una copia de los pasos, así que cambiar el flujo no altera las solicitudes en curso.
- Quien solicita no puede aprobar.
- Se notifica a los aprobadores de cada paso.

Reacciona al resultado en el módulo dueño:

```csharp
public sealed class OnPurchaseDecided(IRepository<PurchaseOrder, Guid> orders) : IDomainEventHandler<ApprovalCompletedEvent>
{
    public async Task HandleAsync(ApprovalCompletedEvent e, CancellationToken ct)
    {
        if (e.Workflow != "purchasing.order") return;
        // marcar la orden como aprobada o rechazada
    }
}
```

Cada tenant ajusta los pasos en *Flujos de aprobación*. Los usuarios deciden desde *Aprobaciones*.

## Importar y exportar

Todo recurso publicado con `MapCrud` obtiene dos endpoints:

- `GET /export?format=csv|xlsx`, con el mismo filtro y orden del listado. Requiere `.read`.
- `POST /import` con un archivo CSV o XLSX. Requiere `.create`.

Detalles de la importación:

- Valida cada fila con los validadores del recurso y guarda todo o nada.
- Con `?dryRun=true` solo valida.
- La respuesta `422` lista los errores por fila.
- Las columnas se emparejan por nombre de propiedad.
- Acepta `,` o `;` como separador, y números con punto o coma decimal. Un valor como `1,234` sin otro separador se
  interpreta como decimal: `1.234`.

La exportación neutraliza fórmulas (inyección CSV) y tiene un tope de 50 000 filas. En el front:

```tsx
<DataExchange resource="/hr/employees" params={listParams} permission="hr.employees" onImported={() => list.refetch()} />
```

## Webhooks

```csharp
services.AddWebhookEvent<EmployeeHired>("hr.employee.hired", "Se contrató a una persona.");
```

Cada tenant suscribe sus URL a los eventos públicos. Las entregas van firmadas, se reintentan y quedan registradas. Ver
[Webhooks](webhooks.md).

## Garantías de entrega

El outbox entrega cada evento al menos una vez: si un manejador falla, el mensaje completo se reintenta. Un consumidor
que escribe datos debe ser idempotente. `IEventContext.IdFor("nombre")` devuelve el mismo id en cada reintento del mismo
mensaje; así lo hacen la auditoría y los webhooks.

## Cómo verificar que funcionó

- `dotnet test` en verde: cada capacidad tiene pruebas funcionales en los tres motores.
- En la aplicación, el menú muestra *Archivos*, *Aprobaciones*, *Tareas programadas*, *Numeración*, *Auditoría* y
  *Webhooks* a un administrador.

## Problemas comunes

Ver [troubleshooting.md](../troubleshooting.md).
