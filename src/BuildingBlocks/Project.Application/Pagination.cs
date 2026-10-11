namespace Project.Application;

/// <summary>
/// Paging, sorting and filtering parameters of a list endpoint.
/// <c>Sort</c> is <c>field,-field2</c>; <c>Filter</c> is the DSL <c>Field:op:value</c> joined by <c>;</c>.
/// </summary>
public sealed record PageRequest(
    int Page = 1,
    int PageSize = PaginationDefaults.DefaultPageSize,
    string? Sort = null,
    string? Filter = null,
    bool IncludeTotal = true)
{
    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize(int maxPageSize) =>
        PageSize < 1 ? PaginationDefaults.DefaultPageSize : Math.Min(PageSize, maxPageSize);
}

/// <summary>Page of results. <see cref="Total"/> is null when the caller did not request it.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long? Total)
{
    public int? TotalPages => Total is null || PageSize == 0 ? null : (int)Math.Ceiling(Total.Value / (double)PageSize);
}

public static class PaginationDefaults
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;
}
