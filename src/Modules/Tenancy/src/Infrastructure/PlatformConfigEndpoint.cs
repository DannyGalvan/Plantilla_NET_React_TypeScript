using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.OutputCaching;
using Project.Modules.Tenancy.Application;
using Project.Web;

namespace Project.Modules.Tenancy.Infrastructure;

/// <summary>
/// Anonymous /platform/config endpoint. Resolves branding + localization +
/// features for the current host so a single build serves many brands.
/// Output cached per host with the \`tenant:{id}\` tag.
/// </summary>
public static class PlatformConfigEndpoint
{
    public static IEndpointRouteBuilder MapPlatformConfig(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/platform").AllowAnonymous();

        group.MapGet("/config", async (
            ISettingsProvider settings,
            IFeatureFlags features,
            ITenantCatalog catalog,
            HttpContext http,
            CancellationToken ct) =>
        {
            var host = http.Request.Host.Host;
            var tenant = await catalog.FindByHostAsync(host, ct);
            var branding = await settings.GetAsync<BrandingSettings>(ct);
            var localization = await settings.GetAsync<LocalizationSettings>(ct);
            var tax = await settings.GetAsync<TaxSettings>(ct);
            var security = await settings.GetAsync<SecuritySettings>(ct);

            var featureList = new List<string>();
            if (tenant is not null)
            {
                foreach (var feature in tenant.Features)
                {
                    if (await features.IsEnabledAsync(feature, ct))
                    {
                        featureList.Add(feature);
                    }
                }
            }

            return Results.Ok(new PlatformConfigDto(
                AppName: branding.DisplayName,
                TenancyMode: tenant is null ? "None" : "Shared",
                Tenant: tenant is null ? null : new TenantInfoLite(tenant.Code, tenant.Name),
                Branding: branding,
                Localization: localization,
                Tax: tax,
                Security: security,
                Features: featureList,
                OrganizationEnabled: false));
        }).CacheOutput("tenant");

        return endpoints;
    }
}

public sealed record PlatformConfigDto(
    string AppName,
    string TenancyMode,
    TenantInfoLite? Tenant,
    BrandingSettings Branding,
    LocalizationSettings Localization,
    TaxSettings Tax,
    SecuritySettings Security,
    IReadOnlyList<string> Features,
    bool OrganizationEnabled);

public sealed record TenantInfoLite(string Code, string Name);