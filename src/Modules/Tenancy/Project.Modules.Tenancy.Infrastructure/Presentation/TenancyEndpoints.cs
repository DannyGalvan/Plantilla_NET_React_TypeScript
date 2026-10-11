using System.Text.Json.Nodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Project.Application;
using Project.Application.Organization;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Infrastructure.Tenancy;
using Project.Modules.Tenancy.Application.Settings;
using Project.Modules.Tenancy.Application.Tenants;
using Project.Modules.Tenancy.Contracts;
using Project.Modules.Tenancy.Domain;
using Project.Modules.Tenancy.Infrastructure.Services;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;
using Project.SharedKernel;

namespace Project.Modules.Tenancy.Infrastructure.Presentation;

public sealed record CreateTenantRequest(
    string Code,
    string Name,
    DatabaseIsolation Isolation,
    string? DatabaseProvider,
    string? ConnectionSecretRef,
    string? Plan,
    string? PrimaryDomain);

public sealed record UpdateTenantRequest(string Name, string? Plan);

public sealed record ChangeStatusRequest(TenantState Status);

public sealed record AddDomainRequest(string Host, bool IsPrimary);

public sealed record SetFeatureRequest(bool Enabled);

/// <summary>Public configuration the SPA loads before rendering (brand, locale, features). Resolved by host.</summary>
public sealed record PlatformConfigDto(
    string AppName,
    TenancyMode TenancyMode,
    PlatformTenantDto? Tenant,
    BrandingSettings Branding,
    LocalizationSettings Localization,
    IReadOnlyList<string> Features,
    IReadOnlyList<string> Modules,
    bool OrganizationEnabled,
    IReadOnlyList<string> Languages);

public sealed record PlatformTenantDto(Guid Id, string Code, string Name);

internal static class TenancyEndpoints
{
    public const string PlatformConfigCacheTag = "platform-config";

    public static void Map(IEndpointRouteBuilder api)
    {
        MapTenants(api.MapGroup("tenancy/tenants").WithTags("tenants"));
        MapSettings(api);
        MapPlatformConfig(api);
    }

    private static void MapTenants(RouteGroupBuilder group)
    {
        group.MapGet("/", async ([AsParameters] Web.Crud.MapCrudExtensions.ListQuery query, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListTenantsQuery(new PageRequest(query.Page ?? 1, query.PageSize ?? 20, query.Sort, query.Filter)), ct)).ToHttp())
            .RequirePermission(TenancyPermissions.Tenants.Read)
            .WithName("ListTenants");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetTenantQuery(id), ct)).ToHttp())
            .RequirePermission(TenancyPermissions.Tenants.Read)
            .WithName("GetTenant");

        group.MapPost("/", async (CreateTenantRequest request, ISender sender, EfTenantStore store, IOutputCacheStore output, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CreateTenantCommand(request.Code, request.Name, request.Isolation, request.DatabaseProvider, request.ConnectionSecretRef, request.Plan, request.PrimaryDomain),
                    ct);
                await InvalidateAsync(result, store, output, ct);
                return result.ToCreated(t => $"/api/v1/tenancy/tenants/{t.Id}");
            })
            .RequirePermission(TenancyPermissions.Tenants.Manage)
            .WithName("CreateTenant");

        group.MapPut("/{id:guid}", async (Guid id, UpdateTenantRequest request, ISender sender, EfTenantStore store, IOutputCacheStore output, CancellationToken ct) =>
            {
                var result = await sender.Send(new UpdateTenantCommand(id, request.Name, request.Plan), ct);
                await InvalidateAsync(result, store, output, ct);
                return result.ToHttp();
            })
            .RequirePermission(TenancyPermissions.Tenants.Manage)
            .WithName("UpdateTenant");

        group.MapPost("/{id:guid}/status", async (Guid id, ChangeStatusRequest request, ISender sender, EfTenantStore store, IOutputCacheStore output, CancellationToken ct) =>
            {
                var result = await sender.Send(new ChangeTenantStatusCommand(id, request.Status), ct);
                await InvalidateAsync(result, store, output, ct);
                return result.ToHttp();
            })
            .RequirePermission(TenancyPermissions.Tenants.Manage)
            .WithName("ChangeTenantStatus");

        group.MapPost("/{id:guid}/domains", async (Guid id, AddDomainRequest request, ISender sender, EfTenantStore store, IOutputCacheStore output, CancellationToken ct) =>
            {
                var result = await sender.Send(new AddTenantDomainCommand(id, request.Host, request.IsPrimary), ct);
                await InvalidateAsync(result, store, output, ct);
                return result.ToHttp();
            })
            .RequirePermission(TenancyPermissions.Tenants.Manage)
            .WithName("AddTenantDomain");

        group.MapDelete("/{id:guid}/domains/{domainId:guid}", async (Guid id, Guid domainId, ISender sender, EfTenantStore store, IOutputCacheStore output, CancellationToken ct) =>
            {
                var result = await sender.Send(new RemoveTenantDomainCommand(id, domainId), ct);
                await InvalidateAsync(result, store, output, ct);
                return result.ToHttp();
            })
            .RequirePermission(TenancyPermissions.Tenants.Manage)
            .WithName("RemoveTenantDomain");

        group.MapPut("/{id:guid}/features/{feature}", async (Guid id, string feature, SetFeatureRequest request, ISender sender, EfTenantStore store, IOutputCacheStore output, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetTenantFeatureCommand(id, feature, request.Enabled), ct);
                await InvalidateAsync(result, store, output, ct);
                return result.ToHttp();
            })
            .RequirePermission(TenancyPermissions.Tenants.Manage)
            .WithName("SetTenantFeature");

        group.MapPost("/{id:guid}/provision", async (Guid id, ISender sender, EfTenantStore store, IOutputCacheStore output, CancellationToken ct) =>
            {
                var result = await sender.Send(new ProvisionTenantCommand(id), ct);
                await InvalidateAsync(result, store, output, ct);
                return result.ToHttp();
            })
            .RequirePermission(TenancyPermissions.Tenants.Manage)
            .WithName("ProvisionTenant");
    }

    private static void MapSettings(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("tenancy/settings").WithTags("settings");

        group.MapGet("/schema", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetSettingsSchemaQuery(), ct)).ToHttp())
            .RequirePermission(TenancyPermissions.Settings.Read)
            .WithName("GetSettingsSchema");

        group.MapGet("/{section}", async (string section, SettingScope? scope, Guid? scopeId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetSettingsQuery(section, scope ?? SettingScope.Tenant, scopeId), ct)).ToHttp())
            .RequirePermission(TenancyPermissions.Settings.Read)
            .WithName("GetSettings");

        group.MapPut("/{section}", async (string section, SettingScope? scope, Guid? scopeId, JsonObject values, ISender sender, IOutputCacheStore output, CancellationToken ct) =>
            {
                var result = await sender.Send(new UpdateSettingsCommand(section, scope ?? SettingScope.Tenant, scopeId, values), ct);
                if (result.IsSuccess)
                {
                    await output.EvictByTagAsync(PlatformConfigCacheTag, ct);
                }

                return result.ToHttp();
            })
            .RequirePermission(TenancyPermissions.Settings.Manage)
            .WithName("UpdateSettings");

        var me = api.MapGroup("me/settings").WithTags("me");
        me.MapGet("/{section}", async (string section, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetSettingsQuery(section, SettingScope.User, null), ct)).ToHttp())
            .RequireAuthenticatedUser()
            .WithName("GetMySettings");

        me.MapPut("/{section}", async (string section, JsonObject values, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateSettingsCommand(section, SettingScope.User, null, values), ct)).ToHttp())
            .RequireAuthenticatedUser()
            .WithName("UpdateMySettings");
    }

    private static void MapPlatformConfig(IEndpointRouteBuilder api)
    {
        api.MapGet("platform/config", async (
                ITenantContext tenant,
                ISettingsProvider settings,
                IFeatureFlags features,
                IOrganizationContext organization,
                ModuleCatalog modules,
                IOptions<TenancyOptions> tenancy,
                CancellationToken ct) =>
            {
                var branding = await settings.GetAsync<BrandingSettings>(ct);
                var info = tenant.Tenant;
                return TypedResults.Ok(new PlatformConfigDto(
                    branding.DisplayName,
                    tenancy.Value.Mode,
                    info is null ? null : new PlatformTenantDto(info.Id, info.Code, info.Name),
                    branding,
                    await settings.GetAsync<LocalizationSettings>(ct),
                    await features.GetEnabledAsync(ct),
                    modules.Modules.Select(m => m.Name.ToLowerInvariant()).ToList(),
                    modules.IsLoaded("Organization"),
                    ["es", "en"]));
            })
            .AllowAnonymous()
            .AllowWithoutTenant()
            .CacheOutput(p => p.SetVaryByHost(true).Expire(TimeSpan.FromSeconds(60)).Tag(PlatformConfigCacheTag))
            .WithTags("platform")
            .WithName("GetPlatformConfig");
    }

    private static async Task InvalidateAsync<T>(Result<T> result, EfTenantStore store, IOutputCacheStore output, CancellationToken ct)
        where T : notnull
    {
        if (result is { IsSuccess: true, Value: TenantDto tenant })
        {
            await store.InvalidateAsync(tenant.Id, ct);
            await output.EvictByTagAsync(PlatformConfigCacheTag, ct);
        }
    }
}
