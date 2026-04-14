using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Request
{
    /// <summary>
    /// Defines the <see cref="RolRequest" />
    /// </summary>
    public class RolRequest : IRequest<long?>
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the Description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int? State { get; set; }

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        public long? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        public long? UpdatedBy { get; set; }
    }
}
