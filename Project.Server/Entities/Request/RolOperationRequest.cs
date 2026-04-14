using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Request
{
    /// <summary>
    /// Defines the <see cref="RolOperationRequest" />
    /// </summary>
    public class RolOperationRequest : IRequest<long?>
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// Gets or sets the RolId
        /// </summary>
        public long? RolId { get; set; }

        /// <summary>
        /// Gets or sets the OperationId
        /// </summary>
        public long? OperationId { get; set; }

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
