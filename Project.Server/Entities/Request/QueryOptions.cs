namespace Project.Server.Entities.Request
{
    /// <summary>
    /// Query options for the generic CRUD stack. Replaces the loose parameter list on
    /// <c>IEntityService</c> with a single record-like object so future fields (sort,
    /// include-deleted, total) don't break the signature.
    /// </summary>
    public sealed class QueryOptions
    {
        /// <summary>Filter DSL string (e.g. <c>"Name:like:foo"</c>). Validated against the entity allowlist.</summary>
        public string? Filters { get; init; }

        /// <summary>Navigation properties to include. Max 3, max depth 2.</summary>
        public string[]? Includes { get; init; }

        /// <summary>Property name to sort by. Must be in the entity sort allowlist.</summary>
        public string? SortBy { get; init; }

        /// <summary>Sort direction. Defaults to descending.</summary>
        public bool SortDescending { get; init; } = true;

        public int PageNumber { get; init; } = 1;

        public int PageSize { get; init; } = 30;

        public bool IncludeTotal { get; init; }

        /// <summary>
        /// When true, soft-deleted rows are returned. Requires an explicit operation
        /// (the default endpoint rejects this flag).
        /// </summary>
        public bool IncludeDeleted { get; init; }
    }
}