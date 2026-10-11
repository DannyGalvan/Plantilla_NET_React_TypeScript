using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Project.Application.Abstractions;
using Project.Application.Organization;
using Project.Infrastructure.Outbox;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// One concern applied to the change tracker before saving (audit, soft delete, tenant...).
/// Register additional steps in DI to extend the pipeline without editing it.
/// </summary>
public interface ISaveChangesStep
{
    /// <summary>Lower runs first.</summary>
    int Order { get; }

    void Apply(ModuleDbContext context);
}

/// <summary>Runs every <see cref="ISaveChangesStep"/> in order, once per SaveChanges.</summary>
public sealed class ModuleSaveChangesInterceptor(IEnumerable<ISaveChangesStep> steps) : SaveChangesInterceptor
{
    private readonly ISaveChangesStep[] _steps = steps.OrderBy(s => s.Order).ToArray();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is not ModuleDbContext module)
        {
            return;
        }

        module.ChangeTracker.DetectChanges();
        foreach (var step in _steps)
        {
            step.Apply(module);
        }
    }
}

/// <summary>Turns deletes of <see cref="ISoftDeletable"/> entities into updates.</summary>
public sealed class SoftDeleteStep(ICurrentUser user, TimeProvider clock) : ISaveChangesStep
{
    public int Order => 0;

    public void Apply(ModuleDbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>().Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = clock.GetUtcNow();
            entry.Entity.DeletedBy = user.UserId ?? "system";
        }
    }
}

/// <summary>Fills creation and modification metadata.</summary>
public sealed class AuditStep(ICurrentUser user, TimeProvider clock) : ISaveChangesStep
{
    public int Order => 10;

    public void Apply(ModuleDbContext context)
    {
        var now = clock.GetUtcNow();
        var actor = user.UserId ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = actor;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actor;
                    break;
            }
        }
    }
}

/// <summary>
/// Assigns the current tenant to new rows (values from the request body are never trusted) and refuses to
/// modify rows of another tenant (defense in depth on top of the global filter).
/// </summary>
public sealed class TenantStep : ISaveChangesStep
{
    public int Order => 20;

    public void Apply(ModuleDbContext context)
    {
        var tenantId = context.CurrentTenantId;
        foreach (var entry in context.ChangeTracker.Entries<IMultiTenant>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted && entry.Entity.TenantId != tenantId)
            {
                throw new TenantMismatchException(entry.Entity.TenantId, tenantId);
            }
        }
    }
}

/// <summary>Assigns the active unit to new organization-scoped rows and blocks writes outside the user's scope.</summary>
public sealed class OrganizationStep(IOrganizationContext organization) : ISaveChangesStep
{
    public int Order => 30;

    public void Apply(ModuleDbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<IOrganizationScoped>())
        {
            if (entry.State == EntityState.Added && entry.Entity.OrganizationUnitId == Guid.Empty)
            {
                entry.Entity.OrganizationUnitId = organization.ActiveUnitId ?? Guid.Empty;
            }

            if (entry.State is EntityState.Added or EntityState.Modified
                && organization.Enabled
                && !organization.HasFullAccess
                && !organization.AccessibleUnitIds.Contains(entry.Entity.OrganizationUnitId))
            {
                throw new ForbiddenAccessException("The organization unit is outside of your scope.");
            }
        }
    }
}

/// <summary>Regenerates the concurrency stamp of every written row.</summary>
public sealed class ConcurrencyStampStep : ISaveChangesStep
{
    public int Order => 40;

    public void Apply(ModuleDbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<IHasConcurrencyStamp>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.ConcurrencyStamp = Guid.NewGuid();
        }
    }
}

/// <summary>Moves domain events of the aggregates into the outbox table, in the same transaction.</summary>
public sealed class OutboxStep(TimeProvider clock) : ISaveChangesStep
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public int Order => 100;

    public void Apply(ModuleDbContext context)
    {
        var aggregates = context.ChangeTracker.Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var type = domainEvent.GetType();
                context.OutboxMessages.Add(new OutboxMessage
                {
                    Id = NewId.Next(),
                    TenantId = context.CurrentTenantId,
                    Type = $"{type.FullName}, {type.Assembly.GetName().Name}",
                    Payload = JsonSerializer.Serialize(domainEvent, type, JsonOptions),
                    OccurredAt = clock.GetUtcNow(),
                });
            }

            aggregate.ClearDomainEvents();
        }
    }
}

public sealed class TenantMismatchException(Guid entityTenant, Guid currentTenant)
    : Exception($"Entity of tenant {entityTenant} cannot be written from tenant {currentTenant}.");
