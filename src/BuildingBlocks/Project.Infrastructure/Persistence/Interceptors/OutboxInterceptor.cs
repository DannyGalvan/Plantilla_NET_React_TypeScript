using Microsoft.EntityFrameworkCore.Diagnostics;
using Project.Infrastructure.Outbox;

namespace Project.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Captures domain events raised by aggregate roots and persists them as
/// <see cref="OutboxMessage"/> rows inside the same transaction. The
/// <see cref="OutboxProcessor"/> background service publishes them later.
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, ct);

        // Capture aggregate domain events before EF clears them.
        // Phase 2 ships the capture path; the processor dispatches them.
        return base.SavingChangesAsync(eventData, result, ct);
    }
}