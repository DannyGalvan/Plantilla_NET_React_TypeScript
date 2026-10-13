# Eventos, outbox y trabajos en segundo plano

## Eventos de dominio

Un agregado llama a `Raise(new ProductDiscontinued(Id))`. Al guardar, el paso `OutboxStep` serializa el evento en la
tabla `OutboxMessages` del módulo, dentro de la misma transacción: si la escritura falla, el evento no existe.

## Despacho

`OutboxProcessor<TContext>` (uno por módulo) lee lotes pendientes cada `Outbox:PollingIntervalSeconds`, los reclama con
una concesión (`LockId`, `LockedUntil`) para que varias instancias no los procesen dos veces, y los publica con
`IEventBus`. Cada evento se despacha en su propio alcance con el tenant que lo generó. Los fallos se reintentan hasta
`Outbox:MaxAttempts` y guardan el último error.

## Manejar un evento

```csharp
public sealed class NotifyBuyers(IEmailSender email) : IDomainEventHandler<ProductDiscontinued>
{
    public Task HandleAsync(ProductDiscontinued e, CancellationToken ct) => email.SendAsync(...);
}
```

Se registra solo por convención (`AddModuleConventions`). Si otro módulo consume el evento, defínelo en `Contracts`.

## Cambiar a un broker

Implementa `IEventBus` publicando en RabbitMQ o Azure Service Bus y regístralo en un módulo o en el host; los módulos
no cambian.

## Trabajos por tenant

`ITenantJobRunner.ForEachTenantAsync((services, tenant, ct) => ...)` ejecuta trabajo en cada tenant activo con su propio
alcance, tenant y conexión, con paralelismo acotado. Úsalo dentro de un `BackgroundService`.
