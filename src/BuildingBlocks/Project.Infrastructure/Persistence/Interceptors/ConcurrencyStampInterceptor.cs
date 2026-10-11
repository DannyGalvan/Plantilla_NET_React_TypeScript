using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Generates a fresh <see cref="IHasConcurrencyStamp.ConcurrencyStamp"/> on every
/// modification. The token is a portable Guid instead of a database-specific
/// <c>rowversion</c>.
/// </summary>
public sealed class ConcurrencyStampInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void Stamp(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IHasConcurrencyStamp stamped) continue;

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                stamped.ConcurrencyStamp = Guid.NewGuid();
            }
        }
    }
}