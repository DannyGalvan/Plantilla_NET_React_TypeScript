using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Request
{
    /// <summary>
    /// Defines the <see cref="OperationRequest" />
    /// </summary>
    public class OperationRequest : IRequest<long?>
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// Gets or sets the Guid
        /// </summary>
        public string? Guid { get; set; }

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the Description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the Policy
        /// </summary>
        public string? Policy { get; set; }

        /// <summary>
        /// Gets or sets the Icon
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the Path
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the ModuleId
        /// </summary>
        public long? ModuleId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsVisible
        /// </summary>
        public bool? IsVisible { get; set; }

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int? State { get; set; }

        /// <summary>
        /// Gets or sets the ControllerName
        /// </summary>
        public string? ControllerName { get; set; }

        /// <summary>
        /// Gets or sets the ActionName
        /// </summary>
        public string? ActionName { get; set; }

        /// <summary>
        /// Gets or sets the HttpMethod
        /// </summary>
        public string? HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets the RouteTemplate
        /// </summary>
        public string? RouteTemplate { get; set; }

        /// <summary>
        /// Gets or sets the OperationKey
        /// </summary>
        public string? OperationKey { get; set; }

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
