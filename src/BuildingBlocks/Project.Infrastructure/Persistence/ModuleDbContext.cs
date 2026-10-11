using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Project.Infrastructure.Tenancy;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Base DbContext shared by every module. Combines global filters for the
/// soft-delete, multi-tenant and organization-scoped markers into a single
/// per-entity filter. Concrete modules derive from this and configure their
/// own <c>IEntityTypeConfiguration</c> classes.
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    private readonly ITenantContext _tenant;
    private readonly IOrganizationContext _organization;

    protected ModuleDbContext(
        DbContextOptions options,
        ITenantContext tenant,
        IOrganizationContext organization)
        : base(options)
    {
        _tenant = tenant;
        _organization = organization;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ApplyGlobalFilters(modelBuilder);
    }

    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        var sharedDatabase = _tenant.Mode is TenancyMode.SharedDatabase
            or TenancyMode.Hybrid;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            var parameter = System.Linq.Expressions.Expression.Parameter(clr, "e");

            System.Linq.Expressions.Expression? combined = null;

            if (typeof(ISoftDeletable).IsAssignableFrom(clr))
            {
                var prop = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var body = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(prop, System.Linq.Expressions.Expression.Constant(false)),
                    parameter);
                combined = combined is null ? body : System.Linq.Expressions.Expression.AndAlso(combined, body);
            }

            if (typeof(IMultiTenant).IsAssignableFrom(clr) && sharedDatabase)
            {
                var prop = System.Linq.Expressions.Expression.Property(parameter, nameof(IMultiTenant.TenantId));
                var tenantId = _tenant.TenantId ?? Guid.Empty;
                var body = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(prop, System.Linq.Expressions.Expression.Constant(tenantId, typeof(Guid))),
                    parameter);
                combined = combined is null ? body : System.Linq.Expressions.Expression.AndAlso(combined, body);
            }

            if (typeof(IOrganizationScoped).IsAssignableFrom(clr) && _organization.Enabled)
            {
                var prop = System.Linq.Expressions.Expression.Property(parameter, nameof(IOrganizationScoped.OrganizationUnitId));
                var accessible = _organization.AccessibleUnitIds;
                var containsMethod = typeof(System.Collections.Generic.HashSet<Guid>)
                    .GetMethod("Contains", new[] { typeof(Guid) })!;
                var body = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Constant(accessible), containsMethod, prop),
                    parameter);
                combined = combined is null ? body : System.Linq.Expressions.Expression.AndAlso(combined, body);
            }

            if (combined is not null)
            {
                var lambda = System.Linq.Expressions.Expression.Lambda(combined, parameter);
                modelBuilder.Entity(clr).HasQueryFilter(lambda);
            }
        }
    }
}