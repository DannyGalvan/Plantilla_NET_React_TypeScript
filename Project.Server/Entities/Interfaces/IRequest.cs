namespace Project.Server.Entities.Interfaces
{
    /// <summary>
    /// Defines the <see cref="IRequest{TId}" />
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    public interface IRequest<TId>
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        TId Id { get; set; }

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        long? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        long? UpdatedBy { get; set; }
    }
}
