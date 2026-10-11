namespace Project.Application;

/// <summary>
/// Paged query parameters with optional keyset cursor.
/// </summary>
public sealed record PageRequest(int Page = 1, int PageSize = 20, string? Sort = null, string? Filter = null, string? Cursor = null);

/// <summary>
/// Paged result envelope.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long? Total, string? NextCursor);