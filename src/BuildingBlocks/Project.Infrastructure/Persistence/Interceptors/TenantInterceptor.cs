using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Project.Infrastructure.Tenancy;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Assigns <see cref="IMultiTenant.TenantId"/> on insert and throws if a
/// modified entity carries a tenant id different from the current one
/// (defense in depth against accidental cross-tenant writes).
/// </summary>
public sealed class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenant;

    public TenantInterceptor(ITenantContext tenant)
    {
        _tenant = tenant;
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

        var expected = _tenant.TenantId;
        if (expected is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IMultiTenant entity) continue;

            if (entry.State == EntityState.Added && entity.TenantId == Guid.Empty)
            {
                entity.TenantId = expected.Value;
                continue;
            }

            if (entry.State == EntityState.Modified && entity.TenantId != expected.Value)
            {
                throw new TenantMismatchException(entity.TenantId, expected.Value);
            }
        }
    }
}

public sealed class TenantMismatchException : Exception
{
    public Guid EntityTenantId { get; }
    public Guid CurrentTenantId { get; }

    public TenantMismatchException(Guid entity, Guid current)
        : base($"Entity TenantId {entity} does not match current tenant {current}.")
    {
        EntityTenantId = entity;
        CurrentTenantId = current;
    }
}