using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Application.Organization;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Tenancy;
using Project.Modules.Tenancy.Application.Settings;
using Project.Modules.Tenancy.Application.Tenants;
using Project.Modules.Tenancy.Contracts;
using Project.Modules.Tenancy.Domain;
using Project.Modules.Tenancy.Infrastructure.Persistence;
using Project.SharedKernel;

namespace Project.Modules.Tenancy.Infrastructure.Services;

/// <summary>
/// Tenant catalog backed by the platform database and HybridCache (memory + Redis). Unknown hosts and codes are
/// not cached, so a new tenant is visible immediately; changes invalidate the <c>tenants</c> tag.
/// </summary>
public sealed class EfTenantStore(TenancyDbContext db, HybridCache cache, DedicatedConnectionCache connections) : ITenantStore
{
    public const string CacheTag = "tenants";
    private static readonly HybridCacheEntryOptions Entry = new() { Expiration = TimeSpan.FromMinutes(10), LocalCacheExpiration = TimeSpan.FromMinutes(1) };

    public ValueTask<TenantInfo?> FindByIdAsync(Guid id, CancellationToken ct) =>
        GetAsync($"tenant:id:{id:N}", q => q.Where(t => t.Id == id), ct);

    public ValueTask<TenantInfo?> FindByCodeAsync(string code, CancellationToken ct)
    {
        var normalized = code.Trim().ToLowerInvariant();
        return GetAsync($"tenant:code:{normalized}", q => q.Where(t => t.Code == normalized), ct);
    }

    public ValueTask<TenantInfo?> FindByHostAsync(string host, CancellationToken ct)
    {
        var normalized = host.Trim().ToLowerInvariant();
        return GetAsync($"tenant:host:{normalized}", q => q.Where(t => t.Domains.Any(d => d.Host == normalized)), ct);
    }

    public async Task<IReadOnlyList<TenantInfo>> ListActiveAsync(CancellationToken ct)
    {
        var tenants = await db.Tenants.AsNoTracking().Include(t => t.Features)
            .Where(t => t.Status == TenantState.Active)
            .OrderBy(t => t.Code)
            .ToListAsync(ct);
        return tenants.Select(ToInfo).ToList();
    }

    public async Task InvalidateAsync(Guid tenantId, CancellationToken ct)
    {
        connections.Remove(tenantId);
        await cache.RemoveByTagAsync(CacheTag, ct);
    }

    private async ValueTask<TenantInfo?> GetAsync(string key, Func<IQueryable<Tenant>, IQueryable<Tenant>> filter, CancellationToken ct)
    {
        var info = await cache.GetOrCreateAsync(
            key,
            async token =>
            {
                var tenant = await filter(db.Tenants.AsNoTracking().Include(t => t.Features)).FirstOrDefaultAsync(token);
                return tenant is null ? null : ToInfo(tenant);
            },
            Entry,
            [CacheTag],
            ct);

        if (info is null)
        {
            await cache.RemoveAsync(key, ct);
        }

        return info;
    }

    private static TenantInfo ToInfo(Tenant tenant) => new(
        tenant.Id,
        tenant.Code,
        tenant.Name,
        (TenantStatus)(int)tenant.Status,
        tenant.Isolation == DatabaseIsolation.Dedicated ? TenantIsolation.Dedicated : TenantIsolation.Shared,
        tenant.DatabaseProvider,
        tenant.ConnectionSecretRef,
        tenant.Plan,
        tenant.Features.Where(f => f.Enabled).Select(f => f.Feature).ToList());
}

/// <summary>Invalidates the tenant cache when a tenant changes (dispatched from the outbox).</summary>
public sealed class TenantChangedHandler(EfTenantStore store, HybridCache cache) : IDomainEventHandler<TenantChanged>
{
    public async Task HandleAsync(TenantChanged domainEvent, CancellationToken ct)
    {
        await store.InvalidateAsync(domainEvent.TenantId, ct);
        await cache.RemoveByTagAsync(CascadingSettingsProvider.CacheTag, ct);
    }
}

/// <summary>Invalidates resolved settings when an override changes.</summary>
public sealed class SettingsChangedHandler(HybridCache cache) : IDomainEventHandler<SettingsChanged>
{
    public Task HandleAsync(SettingsChanged domainEvent, CancellationToken ct) => cache.RemoveByTagAsync(CascadingSettingsProvider.CacheTag, ct).AsTask();
}

/// <summary>
/// Cascading settings: code defaults, then <c>Settings:{key}</c> from configuration, then System, Tenant, the
/// organization units from the root to the active one, and finally the user. The most specific value wins.
/// </summary>
public sealed class CascadingSettingsProvider(
    TenancyDbContext db,
    HybridCache cache,
    IConfiguration configuration,
    ITenantContext tenant,
    IOrganizationContext organization,
    ICurrentUser user,
    IEnumerable<SettingsSectionDescriptor> sections) : ISettingsProvider, ISettingsReader
{
    public const string CacheTag = "settings";

    public async ValueTask<T> GetAsync<T>(CancellationToken ct)
        where T : class, ISettingsSection, new()
    {
        var section = sections.FirstOrDefault(s => s.Type == typeof(T)) ?? new SettingsSectionDescriptor(T.SectionKey, typeof(T), T.AllowUserOverride);
        var json = await GetEffectiveAsync(section, ct);
        return json.Deserialize<T>(SettingsJson.Options) ?? new T();
    }

    public async Task<JsonObject> GetEffectiveAsync(SettingsSectionDescriptor section, CancellationToken ct)
    {
        var effective = JsonSerializer.SerializeToNode(Bind(section), section.Type, SettingsJson.Options)!.AsObject();
        var tenantId = tenant.TenantId ?? TenantDefaults.DefaultTenantId;
        Guid? userId = section.AllowUserOverride && Guid.TryParse(user.UserId, out var parsed) ? parsed : null;
        var path = organization.Enabled ? organization.ActiveUnitPath : [];

        var key = $"settings:{section.Key}:{tenantId:N}:{string.Join(',', path.Select(p => p.ToString("N")))}:{userId:N}";
        var layers = await cache.GetOrCreateAsync(
            key,
            async token => await LoadLayersAsync(section.Key, tenantId, path, userId, token),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10), LocalCacheExpiration = TimeSpan.FromMinutes(1) },
            [CacheTag],
            ct);

        foreach (var layer in layers)
        {
            SettingsJson.Merge(effective, JsonNode.Parse(layer)!.AsObject());
        }

        return effective;
    }

    public Task InvalidateAsync(CancellationToken ct) => cache.RemoveByTagAsync(CacheTag, ct).AsTask();

    private object Bind(SettingsSectionDescriptor section)
    {
        var instance = section.CreateDefault();
        configuration.GetSection($"Settings:{section.Key}").Bind(instance);
        return instance;
    }

    private async Task<List<string>> LoadLayersAsync(string sectionKey, Guid tenantId, IReadOnlyList<Guid> path, Guid? userId, CancellationToken ct)
    {
        var unitIds = path.ToList();
        var rows = await db.SettingValues.AsNoTracking()
            .Where(v => v.SectionKey == sectionKey
                && (v.Scope == "System"
                    || (v.Scope == "Tenant" && v.TenantId == tenantId)
                    || (v.Scope == "OrganizationUnit" && v.TenantId == tenantId && unitIds.Contains(v.ScopeId!.Value))
                    || (userId != null && v.Scope == "User" && v.TenantId == tenantId && v.ScopeId == userId)))
            .Select(v => new { v.Scope, v.ScopeId, v.Json })
            .ToListAsync(ct);

        var ordered = new List<string>();
        ordered.AddRange(rows.Where(r => r.Scope == "System").Select(r => r.Json));
        ordered.AddRange(rows.Where(r => r.Scope == "Tenant").Select(r => r.Json));
        foreach (var unit in unitIds)
        {
            ordered.AddRange(rows.Where(r => r.Scope == "OrganizationUnit" && r.ScopeId == unit).Select(r => r.Json));
        }

        ordered.AddRange(rows.Where(r => r.Scope == "User").Select(r => r.Json));
        return ordered;
    }
}

/// <summary>Feature flags: configuration defaults (<c>Features:{name}</c>) overridden per tenant.</summary>
public sealed class TenantFeatureFlags(IConfiguration configuration, ITenantContext tenant, ITenantStore store) : IFeatureFlags
{
    public async ValueTask<bool> IsEnabledAsync(string feature, CancellationToken ct) =>
        (await GetEnabledAsync(ct)).Contains(feature, StringComparer.OrdinalIgnoreCase);

    public async ValueTask<IReadOnlyList<string>> GetEnabledAsync(CancellationToken ct)
    {
        var enabled = configuration.GetSection("Features").GetChildren()
            .Where(c => bool.TryParse(c.Value, out var on) && on)
            .Select(c => c.Key.ToLowerInvariant())
            .ToHashSet();

        if (tenant.TenantId is { } tenantId && await store.FindByIdAsync(tenantId, ct) is { } info)
        {
            enabled.UnionWith(info.Features);
        }

        return enabled.Order().ToList();
    }
}

/// <summary>Provisions a dedicated tenant through the shared database initializer.</summary>
public sealed class TenantProvisioner(
    TenancyDbContext db,
    DatabaseInitializer initializer,
    EfTenantStore store,
    ILogger<TenantProvisioner> logger) : ITenantProvisioner
{
    public async Task<Result> ProvisionAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.Include(t => t.Features).FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
        {
            return Error.NotFound("tenant.not_found");
        }

        await store.InvalidateAsync(tenantId, ct);
        var info = new TenantInfo(
            tenant.Id,
            tenant.Code,
            tenant.Name,
            TenantStatus.Provisioning,
            tenant.Isolation == DatabaseIsolation.Dedicated ? TenantIsolation.Dedicated : TenantIsolation.Shared,
            tenant.DatabaseProvider,
            tenant.ConnectionSecretRef,
            tenant.Plan,
            []);

        try
        {
            await initializer.InitializeTenantAsync(info, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Provisioning failed for tenant {Tenant}", tenant.Code);
            return Error.Failure("tenant.provisioning_failed", "The tenant database could not be provisioned. Check the server log.");
        }

        tenant.ChangeStatus(TenantState.Active);
        await db.SaveChangesAsync(ct);
        await store.InvalidateAsync(tenantId, ct);
        return Result.Success();
    }
}

/// <summary>Seeds the implicit default tenant and its development domains (idempotent).</summary>
public sealed class TenancySeeder(IConfiguration configuration) : IDataSeeder
{
    public int Order => 0;

    public SeederScope Scope => SeederScope.Platform;

    public async Task SeedAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<TenancyDbContext>();
        var tenant = await db.Tenants.Include(t => t.Domains).FirstOrDefaultAsync(t => t.Id == TenantDefaults.DefaultTenantId, ct);
        if (tenant is null)
        {
            tenant = Tenant.Create(
                TenantDefaults.DefaultTenantCode,
                configuration["Tenancy:DefaultTenantName"] ?? "Default",
                DatabaseIsolation.Shared,
                databaseProvider: null,
                connectionSecretRef: null,
                plan: null,
                TenantDefaults.DefaultTenantId);
            db.Tenants.Add(tenant);
        }

        var hosts = configuration.GetSection("Tenancy:DefaultTenantDomains").Get<string[]>() ?? ["localhost", "127.0.0.1"];
        foreach (var host in hosts.Select(h => h.Trim().ToLowerInvariant()).Where(h => tenant.Domains.All(d => d.Host != h)))
        {
            if (!await db.Set<TenantDomain>().AnyAsync(d => d.Host == host, ct))
            {
                tenant.AddDomain(host, isPrimary: tenant.Domains.Count == 0);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

public sealed class TenancyOptionsAccessor(IOptions<TenancyOptions> options)
{
    public TenancyMode Mode => options.Value.Mode;
}
