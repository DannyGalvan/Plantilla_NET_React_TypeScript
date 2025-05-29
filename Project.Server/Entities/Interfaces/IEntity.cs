namespace Project.Server.Entities.Interfaces
{
    /// <summary>
    /// Defines the <see cref="IEntity{TIdEntity}" />
    /// </summary>
    /// <typeparam name="TIdEntity"></typeparam>
    public interface IEntity<TIdEntity>
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        TIdEntity Id { get; set; }

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        int State { get; set; }

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        long CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        long? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the CreatedAt
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedAt
        /// </summary>
        DateTime? UpdatedAt { get; set; }
    }
}
