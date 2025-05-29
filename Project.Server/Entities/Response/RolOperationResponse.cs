namespace Project.Server.Entities.Response
{
    public class RolOperationResponse
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
        /// Gets or sets the OperationId
        /// </summary>
        public long OperationId { get; set; }

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
        /// Gets or sets the Rol
        /// </summary>
        public virtual RolResponse? Rol { get; set; }

        /// <summary>
        /// Gets or sets the Operation
        /// </summary>
        public virtual OperationResponse? Operation { get; set; }
    }
}
