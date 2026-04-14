using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Models
{
    /// <summary>
    /// Defines the <see cref="PasswordHistory" />
    /// </summary>
    public class PasswordHistory : IEntity<long>
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
        /// Gets or sets the PasswordHash
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ChangedAt
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the ChangedBy
        /// </summary>
        public long ChangedBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ForceChange
        /// </summary>
        public bool ForceChange { get; set; } = false;

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int State { get; set; } = 1;

        /// <summary>
        /// Gets or sets the CreatedBy (required by IEntity)
        /// </summary>
        public long CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy (required by IEntity)
        /// </summary>
        public long? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the CreatedAt
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
