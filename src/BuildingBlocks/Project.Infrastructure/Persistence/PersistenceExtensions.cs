using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Application.Abstractions;
using Project.Infrastructure.Outbox;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Persistence.Interceptors;
using Project.Infrastructure.Persistence.Providers;
using Project.Infrastructure.Tenancy;
using Project.Web;

namespace Project.Infrastructure;

/// <summary>
/// Wires the persistence infrastructure for a module. Each module calls this
/// once with its concrete <see cref="ModuleDbContext"/>.
/// </summary>
public static class PersistenceExtensions
{
    /// <summary>
    /// Registers <see cref="IDatabaseProvider"/> based on
    /// <c>Database:Provider</c>. The selection is keyed by name so multiple
    /// providers can coexist (e.g. platform DB on SQL Server + a tenant DB on
    /// PostgreSQL in Hybrid mode).
    /// </summary>
    public static IServiceCollection AddDatabaseProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var providerName = configuration["Database:Provider"] ?? "SqlServer";

        services.TryAddSingleton<IDatabaseProvider>(providerName switch
        {
            "SqlServer" => new SqlServerProvider(),
            "PostgreSql" => new PostgreSqlProvider(),
            "MySql" => new MySqlProvider(),
            _ => throw new InvalidOperationException($"Unknown Database:Provider '{providerName}'"),
        });

        return services;
    }

    /// <summary>
    /// Registers a module's <typeparamref name="TContext"/> with the EF
    /// interceptors and the corresponding repositories / unit of work /
    /// Dapper executor.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        string moduleName,
        DataScope scope)
        where TContext : ModuleDbContext
    {
        services.AddDbContext<TContext>((sp, options) =>
        {
            var provider = sp.GetRequiredService<IDatabaseProvider>();
            var config = sp.GetRequiredService<IConfiguration>();
            var connection = config.GetConnectionString(scope == DataScope.Platform ? "Platform" : moduleName)
                ?? config.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Missing connection string for module " + moduleName);

            provider.Configure(
                options,
                connection,
                migrationsAssembly: $"Project.Migrations.{provider.Name}",
                schema: moduleName.ToLowerInvariant());
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();
        services.AddScoped<ISqlExecutor, DapperSqlExecutor<TContext>>();
        services.AddScoped<IDbConnectionFactory, DefaultDbConnectionFactory>();

        services.AddScoped<AuditableInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<ConcurrencyStampInterceptor>();
        services.AddScoped<TenantInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        return services;
    }

    public static IServiceCollection AddTenancy(this IServiceCollection services, TenancyMode mode)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantSetter>(sp => sp.GetRequiredService<TenantContext>());
        return services;
    }

    public static IServiceCollection AddOutboxProcessor(this IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessor>();
        return services;
    }
}