using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Models
{
    /// <summary>
    /// Defines the <see cref="Operation" />
    /// </summary>
    public class Operation : IEntity<long>
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the Guid
        /// </summary>
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Policy
        /// </summary>
        public string Policy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Icon
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Path
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ModuleId
        /// </summary>
        public long ModuleId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsVisible
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// Gets or sets the CreatedAt
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the UpdatedAt
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        public long CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        public long? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the Module
        /// </summary>
        public virtual Module? Module { get; set; }

        /// <summary>
        /// Gets or sets the RolOperations
        /// </summary>
        public virtual ICollection<RolOperation> RolOperations { get; set; } = new List<RolOperation>();
    }
}
