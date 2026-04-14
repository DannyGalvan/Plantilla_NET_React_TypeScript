using System.Text.Json.Serialization;
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
        /// Gets or sets a value indicating whether MustChangePassword
        /// </summary>
        public bool MustChangePassword { get; set; } = false;

        /// <summary>
        /// Gets or sets the LastPasswordChange
        /// </summary>
        public DateTime? LastPasswordChange { get; set; }

        /// <summary>
        /// Gets or sets the FailedLoginAttempts
        /// </summary>
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>
        /// Gets or sets the LockoutEnd
        /// </summary>
        public DateTime? LockoutEnd { get; set; }

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
        /// Gets or sets the LoginAudits
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<LoginAudit> LoginAudits { get; set; } = new List<LoginAudit>();

        /// <summary>
        /// Gets or sets the PasswordHistories
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    }
}