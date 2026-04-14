using System.ComponentModel;

namespace Project.Server.Entities.Request
{
    public class QueryParamsRequest
    {
        [DefaultValue(null)]
        public string? Filters { get; set; }
        [DefaultValue(null)]
        public string? Include { get; set; }
        [DefaultValue(1)]
        public int PageNumber { get; set; }
        [DefaultValue(30)]
        public int PageSize { get; set; }
        [DefaultValue(false)]
        public bool IncludeTotal { get; set; }
    }
}
