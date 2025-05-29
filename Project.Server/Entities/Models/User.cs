using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Models
{
    /// <summary>
    /// Defines the <see cref="User" />
    /// </summary>
    public class User : IEntity<long>
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the RolId
        /// </summary>
        public long RolId { get; set; }


        /// <summary>
        /// Gets or sets the MunicipalityCode
        /// </summary>
        public string? MunicipalityCode { get; set; }

        /// <summary>
        /// Gets or sets the MunicipalityCode
        /// </summary>
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Password
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the IdentificationDocument
        /// </summary>
        public string IdentificationDocument { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the RecoveryToken
        /// </summary>
        public string RecoveryToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the DateToken
        /// </summary>
        public DateTime? DateToken { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the Reset
        /// </summary>
        public bool? Reset { get; set; } = false;

        /// <summary>
        /// Gets or sets the Number
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int State { get; set; } = 1;

        /// <summary>
        /// Gets or sets the CreatedAt
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the UpdatedAt
        /// </summary>
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        public long CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        public long? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the Rol
        /// </summary>
        public virtual Rol? Rol { get; set; }

        /// <summary>
        /// Gets or sets the Municipality
        /// </summary>
        public virtual Municipality? Municipality { get; set; }
    }
}
