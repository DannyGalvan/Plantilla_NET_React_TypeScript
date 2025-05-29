namespace Project.Server.Entities.Models
{
    public class Municipality
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public virtual Country? Country { get; set; }
        public virtual IEnumerable<User> Users { get; set; } = new List<User>();
    }
}
