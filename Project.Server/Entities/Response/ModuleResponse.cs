using System.Text.Json.Serialization;

namespace Project.Server.Entities.Response
{
    public class ModuleResponse
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
        /// Gets or sets the Image
        /// </summary>
        public string Image { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Path
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// Gets or sets the CreatedAt
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UpdatedAt
        /// </summary>
        public string? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        public long CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        public long? UpdatedBy { get; set; }

        /// <summary>
        /// Gets the Operations
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<OperationResponse> Operations { get; } = new List<OperationResponse>();
    }
}
