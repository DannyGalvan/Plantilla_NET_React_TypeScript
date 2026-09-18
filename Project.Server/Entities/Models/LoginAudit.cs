using System.ComponentModel.DataAnnotations.Schema;
using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Models
{
    /// <summary>
    /// Defines the <see cref="LoginAudit" />
    /// </summary>
    public class LoginAudit : IEntity<long>, IOwnedEntity<long>
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        /// <summary>
        /// Alias of <see cref="UserId"/> so the generic ownership resolver
        /// scopes login audits to the user that owns them. Marked NotMapped:
        /// the existing schema keeps <c>UserId</c> as the column.
        /// </summary>
        [NotMapped]
        public long OwnerId
        {
            get => UserId;
            set => UserId = value;
        }

        public string UserName { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool LoginSuccessful { get; set; }
        public string? FailureReason { get; set; }
        public DateTime LoginDate { get; set; } = DateTime.UtcNow;
        public int State { get; set; } = 1;
        public long CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public virtual User? User { get; set; }
    }
}