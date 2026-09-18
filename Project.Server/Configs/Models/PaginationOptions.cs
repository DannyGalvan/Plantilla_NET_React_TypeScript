namespace Project.Server.Configs.Models
{
    /// <summary>
    /// Bounds applied to pagination inputs. Bound by <c>AppSettings</c> validation.
    /// </summary>
    public sealed class PaginationOptions
    {
        public const string SectionName = "Pagination";

        public int DefaultPageSize { get; set; } = 30;
        public int MaxPageSize { get; set; } = 100;
        public int MinPageNumber { get; set; } = 1;

        public int ClampPageSize(int requested) => requested <= 0
            ? DefaultPageSize
            : Math.Min(requested, MaxPageSize);

        public int ClampPageNumber(int requested) => requested < MinPageNumber
            ? MinPageNumber
            : requested;
    }
}