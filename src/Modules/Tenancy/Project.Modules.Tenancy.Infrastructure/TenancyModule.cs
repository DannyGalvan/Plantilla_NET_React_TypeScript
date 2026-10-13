using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Tenancy.Application;
using Project.Modules.Tenancy.Application.Settings;
using Project.Modules.Tenancy.Application.Tenants;
using Project.Modules.Tenancy.Infrastructure.Persistence;
using Project.Modules.Tenancy.Infrastructure.Presentation;
using Project.Modules.Tenancy.Infrastructure.Services;
using Project.Web.Modules;

namespace Project.Modules.Tenancy.Infrastructure;

/// <summary>
/// Tenant catalog, domains, features, cascading settings and <c>/platform/config</c>. Always registered:
/// in <c>Tenancy:Mode=None</c> it simply serves the implicit default tenant and its settings.
/// </summary>
public sealed class TenancyModule : IModule
{
    public string Name => "Tenancy";

    public DataScope DataScope => DataScope.Platform;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<TenancyDbContext>(Name, DataScope);
        services.AddRepositoriesFor<TenancyDbContext>();
        services.AddModuleConventions(typeof(TenancyPermissionProvider).Assembly, typeof(TenancyModule).Assembly);

        services.AddScoped<EfTenantStore>();
        services.AddScoped<ITenantStore>(sp => sp.GetRequiredService<EfTenantStore>());
        services.AddScoped<CascadingSettingsProvider>();
        services.AddScoped<ISettingsProvider>(sp => sp.GetRequiredService<CascadingSettingsProvider>());
        services.AddScoped<ISettingsReader>(sp => sp.GetRequiredService<CascadingSettingsProvider>());
        services.AddScoped<IFeatureFlags, TenantFeatureFlags>();
        services.AddScoped<ITenantProvisioner, TenantProvisioner>();
        services.AddScoped<SettingsScopeGuard>();
        services.AddSingleton<ISettingsValidator, DefaultSettingsValidator>();
        services.AddScoped<IDataSeeder, TenancySeeder>();
    }

    public void MapEndpoints(IEndpointRouteBuilder api) => TenancyEndpoints.Map(api);
}
