using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Project.Application.Abstractions;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Converts <see cref="EntityState.Deleted"/> entries that implement
/// <see cref="ISoftDeletable"/> into <see cref="EntityState.Modified"/> with
/// <c>IsDeleted = true</c>. The actual <c>DELETE</c> is never issued.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _user;
    private readonly TimeProvider _clock;

    public SoftDeleteInterceptor(ICurrentUser user, TimeProvider clock)
    {
        _user = user;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ConvertDeletes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        ConvertDeletes(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void ConvertDeletes(DbContext? context)
    {
        if (context is null) return;

        var now = _clock.GetUtcNow();
        var actor = _user.UserId ?? "system";

        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted))
        {
            if (entry.Entity is not ISoftDeletable soft) continue;

            entry.State = EntityState.Modified;
            soft.IsDeleted = true;
            soft.DeletedAt = now;
            soft.DeletedBy = actor;
        }
    }
}