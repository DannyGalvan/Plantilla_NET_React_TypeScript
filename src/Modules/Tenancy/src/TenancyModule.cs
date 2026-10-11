using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Tenancy.Application;
using Project.Modules.Tenancy.Infrastructure;
using Project.Web;

namespace Project.Modules.Tenancy;

/// <summary>
/// Tenancy module. \`DataScope=Platform\` so the catalog lives in the
/// platform connection string. Endpoints under /api/v1/tenancy/* are
/// SuperAdmin-only (the permission key is tenancy.tenants.*); /platform/*
/// settings are per-tenant (tenancy.settings.manage).
/// </summary>
public sealed class TenancyModule : IModule
{
    public string Name => "Tenancy";
    public DataScope DataScope => DataScope.Platform;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantCatalog, EfTenantCatalog>();
        services.AddSingleton<ITenantSettingStore, NullSettingStore>();
        services.AddSingleton<ISettingsProvider, CascadingSettingsProvider>();
        services.AddSingleton<IFeatureFlags, NullFeatureFlags>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPlatformConfig();
    }
}

internal sealed class NullSettingStore : ITenantSettingStore
{
    public ValueTask<string?> GetAsync(string scope, Guid? tenantId, Guid? orgUnitId, Guid? userId, string sectionKey, CancellationToken ct) =>
        ValueTask.FromResult<string?>(null);

    public Task SetAsync(string scope, Guid? tenantId, Guid? orgUnitId, Guid? userId, string sectionKey, string json, CancellationToken ct) =>
        Task.CompletedTask;
}

internal sealed class NullFeatureFlags : IFeatureFlags
{
    public ValueTask<bool> IsEnabledAsync(string feature, CancellationToken ct) => ValueTask.FromResult(true);
}

internal sealed class NullCurrentTenant : ICurrentTenantAccessor
{
    public Guid? TenantId => null;
}

internal sealed class NullCurrentOrganization : ICurrentOrganizationAccessor
{
    public Guid? OrganizationUnitId => null;
}

internal sealed class NullCurrentUser : ICurrentUserAccessor
{
    public Guid? UserId => null;
}