# Eventos, outbox y trabajos en segundo plano

## Eventos de dominio

Un agregado llama a `Raise(new ProductDiscontinued(Id))`. Al guardar, el paso `OutboxStep` serializa el evento en la
tabla `OutboxMessages` del módulo, dentro de la misma transacción: si la escritura falla, el evento no existe.

## Despacho

Un único `OutboxDispatcher` por instancia recorre, uno tras otro, el outbox de cada módulo cada
`Outbox:PollingIntervalSeconds`. Es secuencial a propósito: usa una conexión a la vez, aunque haya muchos módulos y
muchas bases de tenant.

Cada lote se procesa así:

- Los mensajes se reclaman con una concesión (`LockId`, `LockedUntil`), para que varias instancias no procesen el mismo
  dos veces.
- Se publican con `IEventBus`. Cada evento se despacha en su propio alcance, con el tenant que lo generó.
- Los fallos se reintentan hasta `Outbox:MaxAttempts` y guardan el último error.

La entrega es **al menos una vez**: si un manejador falla, el mensaje completo se reintenta y los manejadores que ya
terminaron vuelven a correr. Un manejador que escribe datos debe ser idempotente. `IEventContext.IdFor("nombre")`
devuelve el mismo id en cada reintento del mismo mensaje: úsalo como id de lo que insertas y omite la escritura si ya
existe.

## Manejar un evento

```csharp
public sealed class NotifyBuyers(IEmailSender email) : IDomainEventHandler<ProductDiscontinued>
{
    public Task HandleAsync(ProductDiscontinued e, CancellationToken ct) => email.SendAsync(...);
}
```

Se registra solo por convención (`AddModuleConventions`). Si otro módulo consume el evento, defínelo en `Contracts`.

## Observar todos los eventos

`IDomainEventObserver` recibe cada evento despachado, sea cual sea su tipo, después de los manejadores tipados. Lo usa
el módulo Webhooks para publicar hacia afuera los eventos declarados con `AddWebhookEvent`. Sirve también para
analítica o integraciones.

## Cambiar a un broker

Implementa `IEventBus` publicando en RabbitMQ o Azure Service Bus y regístralo en un módulo o en el host; los módulos
no cambian.

## Tareas programadas

Para trabajo recurrente (cierres, recordatorios, limpiezas) declara un `IRecurringJob` con `AddRecurringJob`. El módulo
`Jobs` lo agenda con cron por tenant, en su zona horaria, con lease distribuido, historial y ejecución manual. Ver
[Capacidades transversales](capacidades-transversales.md#tareas-programadas).

## Trabajo por tenant a medida

- `ITenantJobRunner.ForEachTenantAsync((services, tenant, ct) => ...)` ejecuta trabajo en cada tenant activo, con su
  propio alcance, tenant y conexión, y con paralelismo acotado.
- `RunForTenantAsync(tenantId, ...)` hace lo mismo para un solo tenant. Sirve para callbacks OAuth, entregas o
  reprocesos.
