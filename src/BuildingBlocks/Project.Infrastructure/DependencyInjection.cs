using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Organization;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Infrastructure.Outbox;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Persistence.Providers;
using Project.Infrastructure.Services;
using Project.Infrastructure.Tenancy;

namespace Project.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the shared infrastructure. Modules registered afterwards may replace any default
    /// (organization context, settings, feature flags, tenant store) simply by registering their own implementation.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationCore(configuration);
        services.AddCoreSettingsSections();

        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<MigrationOptions>().Bind(configuration.GetSection(MigrationOptions.SectionName)).ValidateDataAnnotations();
        services.AddOptions<TenancyOptions>().Bind(configuration.GetSection(TenancyOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<OutboxOptions>().Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName)).ValidateDataAnnotations();

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();

        // Database engines (strategies). Add a new engine by registering another IDatabaseProvider.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, SqlServerProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, PostgreSqlProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, MySqlProvider>());
        services.TryAddSingleton<DatabaseProviderRegistry>();

        // Tenancy.
        services.TryAddScoped<TenantContext>();
        services.TryAddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.TryAddScoped<ITenantSetter>(sp => sp.GetRequiredService<TenantContext>());
        services.TryAddSingleton<DedicatedConnectionCache>();
        services.TryAddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
        services.TryAddSingleton<ISecretStore, ConfigurationSecretStore>();
        services.TryAddSingleton<ITenantJobRunner, TenantJobRunner>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, HostTenantResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, SubdomainTenantResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, ClaimTenantResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, HeaderTenantResolver>());

        // Persistence pipeline.
        services.TryAddScoped<ModuleDbContextDependencies>();
        services.TryAddScoped<ModuleSaveChangesInterceptor>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesStep, SoftDeleteStep>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesStep, AuditStep>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesStep, TenantStep>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesStep, OrganizationStep>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesStep, ConcurrencyStampStep>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesStep, OutboxStep>());
        services.TryAddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.TryAddSingleton<DatabaseInitializer>();

        // Cross-cutting defaults.
        var redis = configuration["Cache:Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = redis);
        }

        services.AddHybridCache();
        services.TryAddScoped<ICacheService, HybridCacheService>();
        services.TryAddScoped<ICurrentUser, HttpCurrentUser>();
        services.TryAddScoped<IOrganizationContext, NoOpOrganizationContext>();
        services.TryAddScoped<IOrganizationDirectory, NoOpOrganizationDirectory>();
        services.TryAddScoped<ISettingsProvider, ConfigurationSettingsProvider>();
        services.TryAddScoped<IFeatureFlags, ConfigurationFeatureFlags>();
        services.TryAddScoped<IEventBus, InProcessEventBus>();

        if (string.IsNullOrWhiteSpace(configuration["Email:Host"]))
        {
            services.TryAddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.TryAddSingleton<IEmailSender, SmtpEmailSender>();
        }

        return services;
    }
}
