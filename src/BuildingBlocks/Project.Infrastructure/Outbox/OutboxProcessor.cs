using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Project.Infrastructure.Outbox;

/// <summary>
/// Reads <see cref="OutboxMessage"/> rows in batches using the dialect's
/// skip-locked hint (safe with multiple instances) and publishes them via
/// the in-process Mediator notification publisher. Replaceable by a broker
/// implementation of <c>IEventBus</c>.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeProvider _clock;

    public OutboxProcessor(ILogger<OutboxProcessor> logger, TimeProvider clock)
    {
        _logger = logger;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Phase 2: only the loop skeleton. The actual read+publish uses
                // the module's DbContext (own schema) and the IEventBus.
                _logger.LogDebug("Outbox tick at {Now}", _clock.GetUtcNow());
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor failed; will retry next tick");
            }
        }
    }
}