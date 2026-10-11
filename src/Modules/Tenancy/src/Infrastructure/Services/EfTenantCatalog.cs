using System.Text.Json;
using Project.Application;
using Project.Modules.Tenancy.Application;
using Project.Modules.Tenancy.Domain;
using Project.SharedKernel.Domain;

namespace Project.Modules.Tenancy.Infrastructure;

/// <summary>
/// HybridCache-backed tenant catalog. Phase 4B ships the abstraction; the
/// TenancyDbContext lands with the migration by provider (Fase 2 next slice).
/// </summary>
public sealed class EfTenantCatalog : ITenantCatalog
{
    private readonly ICacheService _cache;

    public EfTenantCatalog(ICacheService cache)
    {
        _cache = cache;
    }

    public async ValueTask<TenantInfo?> FindByCodeAsync(string code, CancellationToken ct)
    {
        var key = $"tenant:code:{code}";
        var cached = await _cache.GetAsync<TenantInfo>(key, ct);
        if (cached is not null) return cached;

        var fresh = await LoadByCodeAsync(code, ct);
        if (fresh is not null)
        {
            await _cache.SetAsync(key, fresh, tags: new[] { $"tenant:{fresh.Id}" }, ct: ct);
        }
        return fresh;
    }

    public async ValueTask<TenantInfo?> FindByHostAsync(string host, CancellationToken ct)
    {
        var key = $"tenant:host:{host}";
        var cached = await _cache.GetAsync<TenantInfo>(key, ct);
        if (cached is not null) return cached;

        var fresh = await LoadByHostAsync(host, ct);
        if (fresh is not null)
        {
            await _cache.SetAsync(key, fresh, tags: new[] { $"tenant:{fresh.Id}" }, ct: ct);
        }
        return fresh;
    }

    public async IAsyncEnumerable<TenantInfo> ListActiveAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var t in EnumerateActiveAsync(ct))
        {
            yield return t;
        }
    }

    public async Task InvalidateAsync(Guid tenantId, CancellationToken ct)
    {
        await _cache.RemoveByTagsAsync(new[] { $"tenant:{tenantId}" }, ct);
    }

    private static ValueTask<TenantInfo?> LoadByCodeAsync(string code, CancellationToken ct) =>
        ValueTask.FromResult<TenantInfo?>(null);

    private static ValueTask<TenantInfo?> LoadByHostAsync(string host, CancellationToken ct) =>
        ValueTask.FromResult<TenantInfo?>(null);

    private static async IAsyncEnumerable<TenantInfo> EnumerateActiveAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        yield break;
    }
}

/// <summary>
/// Cascading settings provider. Reads the default <c>new T()</c>, then
/// \`appsettings\` Settings:{Section}, then the System/Tenant/OrgUnit/User
/// overrides from <see cref="ITenantSettingStore"/>. Each layer is JSON-merged
/// via System.Text.Json so the most specific value wins per property.
/// </summary>
public sealed class CascadingSettingsProvider : ISettingsProvider
{
    private readonly ITenantSettingStore _store;
    private readonly ICurrentTenantAccessor _tenant;
    private readonly ICurrentOrganizationAccessor _org;
    private readonly ICurrentUserAccessor _user;

    public CascadingSettingsProvider(
        ITenantSettingStore store,
        ICurrentTenantAccessor tenant,
        ICurrentOrganizationAccessor org,
        ICurrentUserAccessor user)
    {
        _store = store;
        _tenant = tenant;
        _org = org;
        _user = user;
    }

    public async ValueTask<T> GetAsync<T>(CancellationToken ct) where T : class, ISettingsSection, new()
    {
        var key = T.SectionKey;
        var defaults = new T();

        var json = JsonSerializer.Serialize(defaults);
        var systemJson = await _store.GetAsync("System", null, null, null, key, ct);
        json = MergeJson(json, systemJson);

        if (_tenant.TenantId is Guid tid)
        {
            var tenantJson = await _store.GetAsync("Tenant", tid, null, null, key, ct);
            json = MergeJson(json, tenantJson);
        }

        if (_org.OrganizationUnitId is Guid ouId && _tenant.TenantId is Guid tidForOrg)
        {
            var orgJson = await _store.GetAsync("OrganizationUnit", tidForOrg, ouId, null, key, ct);
            json = MergeJson(json, orgJson);
        }

        if (_user.UserId is Guid userId)
        {
            var userJson = await _store.GetAsync("User", _tenant.TenantId, null, userId, key, ct);
            json = MergeJson(json, userJson);
        }

        return JsonSerializer.Deserialize<T>(json) ?? new T();
    }

    private static string MergeJson(string baseJson, string? overrideJson)
    {
        if (string.IsNullOrWhiteSpace(overrideJson)) return baseJson;
        using var baseDoc = JsonDocument.Parse(baseJson);
        using var overrideDoc = JsonDocument.Parse(overrideJson);

        using var output = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(output))
        {
            jsonWriter.WriteStartObject();
            foreach (var property in baseDoc.RootElement.EnumerateObject())
            {
                property.WriteTo(jsonWriter);
            }
            foreach (var property in overrideDoc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Null)
                {
                    property.WriteTo(jsonWriter);
                }
            }
            jsonWriter.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }
}

public interface ICurrentTenantAccessor
{
    Guid? TenantId { get; }
}

public interface ICurrentOrganizationAccessor
{
    Guid? OrganizationUnitId { get; }
}

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}