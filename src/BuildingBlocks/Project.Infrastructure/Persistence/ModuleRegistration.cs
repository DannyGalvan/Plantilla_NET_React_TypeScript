using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Application.Tenancy;
using Project.Infrastructure.Outbox;
using Project.Infrastructure.Persistence.Providers;
using Project.Infrastructure.Tenancy;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence;

/// <summary>Metadata of a registered module DbContext, used by the migrator and the outbox.</summary>
public sealed record ModuleDbContextRegistration(Type ContextType, string Module, DataScope Scope);

public static class ModuleRegistrationExtensions
{
    /// <summary>
    /// Registers a module DbContext. The connection (and even the engine) is resolved per scope through
    /// <see cref="ITenantConnectionResolver"/>, so the same registration serves every tenancy mode.
    /// Also registers its outbox processor.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string module, DataScope scope)
        where TContext : ModuleDbContext
    {
        services.AddSingleton(new ModuleDbContextRegistration(typeof(TContext), module, scope));

        services.AddDbContext<TContext>((sp, options) =>
        {
            var database = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var (provider, connectionString) = sp.GetRequiredService<ITenantConnectionResolver>().Resolve(module, scope);

            provider.Configure(options, connectionString, new ProviderSettings(
                module.ToLowerInvariant(),
                $"Project.Migrations.{provider.Name}",
                database.CommandTimeoutSeconds,
                database.MaxRetryCount));

            options.AddInterceptors(sp.GetRequiredService<ModuleSaveChangesInterceptor>());

            // Failed commands surface as exceptions (logged once by the exception handler or the caller).
            options.ConfigureWarnings(w => w.Log((Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError, Microsoft.Extensions.Logging.LogLevel.Debug)));
            if (database.EnableSensitiveDataLogging && sp.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddHostedService<OutboxProcessor<TContext>>();
        return services;
    }

    /// <summary>Registers <see cref="IRepository{T,TId}"/> for an aggregate of the module.</summary>
    public static IServiceCollection AddRepository<TContext, T, TId>(this IServiceCollection services)
        where TContext : ModuleDbContext
        where T : AggregateRoot<TId>
        where TId : notnull
    {
        services.TryAddScoped<IRepository<T, TId>, EfRepository<TContext, T, TId>>();
        return services;
    }

    /// <summary>Registers repositories for every aggregate root exposed as a <c>DbSet</c> of the context.</summary>
    public static IServiceCollection AddRepositoriesFor<TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext
    {
        var aggregates = typeof(TContext).GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Distinct();

        foreach (var entity in aggregates)
        {
            for (var current = entity; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                {
                    var id = current.GetGenericArguments()[0];
                    services.TryAddScoped(
                        typeof(IRepository<,>).MakeGenericType(entity, id),
                        typeof(EfRepository<,,>).MakeGenericType(typeof(TContext), entity, id));
                    break;
                }
            }
        }

        return services;
    }

    /// <summary>Exposes Dapper to the module application layer as <c>ISqlExecutor&lt;TModuleKey&gt;</c>.</summary>
    public static IServiceCollection AddSqlExecutor<TModuleKey, TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext
    {
        services.TryAddScoped<ISqlExecutor<TModuleKey>, ModuleSqlExecutor<TModuleKey, TContext>>();
        return services;
    }
}

/// <summary>Plain ADO.NET connections honoring the tenant connection.</summary>
public sealed class DbConnectionFactory(
    ITenantConnectionResolver resolver,
    IEnumerable<ModuleDbContextRegistration> registrations) : IDbConnectionFactory
{
    public DbConnection Create(string moduleName)
    {
        var scope = registrations.FirstOrDefault(r => string.Equals(r.Module, moduleName, StringComparison.OrdinalIgnoreCase))?.Scope
            ?? DataScope.Tenant;
        var (provider, connectionString) = resolver.Resolve(moduleName, scope);
        return provider.CreateConnection(connectionString);
    }
}
