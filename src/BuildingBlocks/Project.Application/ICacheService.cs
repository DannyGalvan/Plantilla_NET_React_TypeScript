namespace Project.Application;

/// <summary>
/// Façade over HybridCache with tag-based invalidation. Keys are automatically
/// prefixed with <c>t:{tenantId}:</c> when tenancy is active.
/// </summary>
public interface ICacheService
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct);
    ValueTask SetAsync<T>(string key, T value, IEnumerable<string>? tags = null, TimeSpan? ttl = null, CancellationToken ct = default);
    ValueTask RemoveAsync(string key, CancellationToken ct);
    ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken ct);
}