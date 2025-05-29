namespace Project.Server.Entities.Models
{
    public class Country
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public IEnumerable<Municipality> Municipalities { get; set; } = new List<Municipality>();
    }
}
