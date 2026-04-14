using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Models
{
    /// <summary>
    /// Defines the <see cref="LoginAudit" />
    /// </summary>
    public class LoginAudit : IEntity<long>
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the UserId
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Gets or sets the UserName
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the IpAddress
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the UserAgent
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether LoginSuccessful
        /// </summary>
        public bool LoginSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the FailureReason
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Gets or sets the LoginDate
        /// </summary>
        public DateTime LoginDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int State { get; set; } = 1;

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        public long CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        public long? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the CreatedAt
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the UpdatedAt
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the User
        /// </summary>
        public virtual User? User { get; set; }
    }
}
