using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Request
{
    public class RegisterRequest : IRequest<long?>
    {
        public long? Id { get; set; }
        public string? MunicipalityCode { get; set; }
        public string? CountryCode { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? IdentificationDocument { get; set; } 
        public string? UserName { get; set; } 
        public string? Number { get; set; }
        public int? State { get; set; } = 1;
        public long? CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
    }
}
