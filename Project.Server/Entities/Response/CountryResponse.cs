using System.Text.Json.Serialization;

namespace Project.Server.Entities.Response
{
    public class CountryResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        [JsonIgnore]
        public IEnumerable<MunicipalityResponse> Municipalities { get; set; } = new List<MunicipalityResponse>();
    }
}
