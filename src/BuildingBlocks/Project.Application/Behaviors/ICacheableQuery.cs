namespace Project.Application.Behaviors;

/// <summary>
/// Marker for queries that should be intercepted by <c>CachingBehavior</c>.
/// Cache key includes <c>t:{tenantId}:</c> prefix when tenancy is active.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    IEnumerable<string>? Tags { get; }
    TimeSpan? Ttl { get; }
}