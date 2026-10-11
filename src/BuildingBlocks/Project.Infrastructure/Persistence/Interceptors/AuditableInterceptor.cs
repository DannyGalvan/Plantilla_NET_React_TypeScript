using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Project.Application.Abstractions;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Sets <c>CreatedAt</c>/<c>CreatedBy</c> and <c>UpdatedAt</c>/<c>UpdatedBy</c>
/// from <see cref="ICurrentUser"/> and <see cref="TimeProvider"/>. Replaces the
/// reflection-based <c>AuditHelper</c> + manual assignments in <c>EntityService</c>.
/// </summary>
public sealed class AuditableInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _user;
    private readonly TimeProvider _clock;

    public AuditableInterceptor(ICurrentUser user, TimeProvider clock)
    {
        _user = user;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var now = _clock.GetUtcNow();
        var actor = _user.UserId ?? "system";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditable auditable) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = actor;
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = actor;
                    break;
                case EntityState.Modified:
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = actor;
                    break;
            }
        }
    }
}