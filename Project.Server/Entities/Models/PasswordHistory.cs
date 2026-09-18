using System.ComponentModel.DataAnnotations.Schema;
using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Models
{
    /// <summary>
    /// Defines the <see cref="PasswordHistory" />
    /// </summary>
    public class PasswordHistory : IEntity<long>, IOwnedEntity<long>
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        /// <summary>
        /// Alias of <see cref="UserId"/> for the generic ownership resolver.
        /// </summary>
        [NotMapped]
        public long OwnerId
        {
            get => UserId;
            set => UserId = value;
        }

        public string PasswordHash { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public long ChangedBy { get; set; }
        public bool ForceChange { get; set; }
        public int State { get; set; } = 1;
        public long CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public virtual User? User { get; set; }
    }
}