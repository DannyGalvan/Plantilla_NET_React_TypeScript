using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Project.Application.Organization;
using Project.Application.Tenancy;
using Project.Infrastructure.Outbox;
using Project.Infrastructure.Persistence.Providers;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence;

/// <summary>Services every module DbContext needs. Scoped.</summary>
public sealed class ModuleDbContextDependencies(
    ITenantContext tenant,
    IOrganizationContext organization,
    DatabaseProviderRegistry providers)
{
    public ITenantContext Tenant { get; } = tenant;

    public IOrganizationContext Organization { get; } = organization;

    public DatabaseProviderRegistry Providers { get; } = providers;
}

/// <summary>Names of the global query filters, usable with <c>IgnoreQueryFilters([...])</c>.</summary>
public static class QueryFilters
{
    public const string SoftDelete = "SoftDelete";
    public const string Tenant = "Tenant";
    public const string Organization = "Organization";
}

/// <summary>
/// Base DbContext of every module. One schema per module (or a table prefix on engines without schemas),
/// global filters for soft delete, tenant and organization scope, portable concurrency tokens, an outbox table
/// and conventions applied by the active <see cref="IDatabaseProvider"/>.
/// <para>
/// The filters read properties of this instance, so EF Core parameterizes them per query: the cached model
/// works for every tenant and every topology.
/// </para>
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options, ModuleDbContextDependencies dependencies) : DbContext(options)
{
    private IDatabaseProvider? _provider;

    /// <summary>Database schema of the module (lower case), for example <c>catalog</c>.</summary>
    public abstract string Schema { get; }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public ITenantContext TenantContext => dependencies.Tenant;

    public IDatabaseProvider Provider => _provider ??= dependencies.Providers.GetByEfProviderName(Database.ProviderName);

    /// <summary>Tenant used by the filter and assigned to new rows.</summary>
    public Guid CurrentTenantId => dependencies.Tenant.TenantId ?? TenantDefaults.DefaultTenantId;

    public bool TenantFilterEnabled => dependencies.Tenant.UsesSharedDatabase;

    public bool OrganizationFilterEnabled =>
        dependencies.Organization.Enabled && !dependencies.Organization.HasFullAccess;

    public List<Guid> AccessibleUnitIds => [.. dependencies.Organization.AccessibleUnitIds];

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
        Provider.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Provider.SupportsSchemas)
        {
            modelBuilder.HasDefaultSchema(Schema);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly, t => IsConfigurationFor(t, GetType()));
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => !e.IsOwned()).ToList())
        {
            ApplyConventions(modelBuilder, entityType);
        }
    }

    /// <summary>
    /// A configuration class belongs to this context when it is not marked with <see cref="DbContextConfigurationAttribute"/>
    /// or when the attribute names this context type (several contexts may share one assembly).
    /// </summary>
    private static bool IsConfigurationFor(Type configurationType, Type contextType)
    {
        var target = configurationType.GetCustomAttributes(typeof(DbContextConfigurationAttribute), false)
            .Cast<DbContextConfigurationAttribute>()
            .FirstOrDefault();
        return target is null || target.ContextType == contextType;
    }

    private void ApplyConventions(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        var clr = entityType.ClrType;
        var builder = modelBuilder.Entity(clr);

        if (!Provider.SupportsSchemas && entityType.GetTableName() is { } table && !table.StartsWith(Schema + "_", StringComparison.Ordinal))
        {
            entityType.SetTableName(MySqlProvider.PrefixedTable(Schema, table));
        }

        if (typeof(IHasConcurrencyStamp).IsAssignableFrom(clr))
        {
            builder.Property(nameof(IHasConcurrencyStamp.ConcurrencyStamp)).IsConcurrencyToken();
        }

        var parameter = Expression.Parameter(clr, "e");
        var context = Expression.Constant(this);

        if (typeof(ISoftDeletable).IsAssignableFrom(clr))
        {
            builder.HasIndex(nameof(ISoftDeletable.IsDeleted));
            var body = Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));
            builder.HasQueryFilter(QueryFilters.SoftDelete, Expression.Lambda(body, parameter));
        }

        if (typeof(IMultiTenant).IsAssignableFrom(clr))
        {
            builder.HasIndex(nameof(IMultiTenant.TenantId));
            var body = Expression.OrElse(
                Expression.Not(Expression.Property(context, nameof(TenantFilterEnabled))),
                Expression.Equal(
                    Expression.Property(parameter, nameof(IMultiTenant.TenantId)),
                    Expression.Property(context, nameof(CurrentTenantId))));
            builder.HasQueryFilter(QueryFilters.Tenant, Expression.Lambda(body, parameter));
        }

        if (typeof(IOrganizationScoped).IsAssignableFrom(clr))
        {
            builder.HasIndex(nameof(IOrganizationScoped.OrganizationUnitId));
            var contains = typeof(List<Guid>).GetMethod(nameof(List<Guid>.Contains))!;
            var body = Expression.OrElse(
                Expression.Not(Expression.Property(context, nameof(OrganizationFilterEnabled))),
                Expression.Call(
                    Expression.Property(context, nameof(AccessibleUnitIds)),
                    contains,
                    Expression.Property(parameter, nameof(IOrganizationScoped.OrganizationUnitId))));
            builder.HasQueryFilter(QueryFilters.Organization, Expression.Lambda(body, parameter));
        }
    }
}

/// <summary>
/// Restricts an <c>IEntityTypeConfiguration</c> to one DbContext when an assembly contains several contexts.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class DbContextConfigurationAttribute(Type contextType) : Attribute
{
    public Type ContextType { get; } = contextType;
}
