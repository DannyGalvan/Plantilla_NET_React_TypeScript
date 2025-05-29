using System.Text.Json.Serialization;

namespace Project.Server.Entities.Response
{
    public class MunicipalityResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public virtual CountryResponse? Country { get; set; }
        [JsonIgnore]
        public virtual IEnumerable<UserResponse> Users { get; set; } = new List<UserResponse>();
    }
}
