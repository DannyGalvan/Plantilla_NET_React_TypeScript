using System.Text.Json.Serialization;

namespace Project.Server.Entities.Response
{
    public class RolResponse
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// Gets or sets the CreatedAt
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        public long CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        public long? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedAt
        /// </summary>
        public string? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the Users
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<UserResponse> Users { get; set; } = new List<UserResponse>();

        /// <summary>
        /// Gets or sets the RolOperations
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<RolOperationResponse> RolOperations { get; set; } = new List<RolOperationResponse>();
    }
}
