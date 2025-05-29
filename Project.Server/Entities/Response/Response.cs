namespace Project.Server.Entities.Response
{
    /// <summary>
    /// Defines the <see cref="Response{TEntity}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class Response<TEntity>
    {
        /// <summary>
        /// Gets or sets a value indicating whether Success
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets or sets the Message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Data
        /// </summary>
        public TEntity? Data { get; set; }

        /// <summary>
        /// Gets or sets the TotalResults
        /// </summary>
        public int TotalResults { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="Response{TEntity, TError}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TError"></typeparam>
    public class Response<TEntity, TError>
    {
        /// <summary>
        /// Gets or sets a value indicating whether Success
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets or sets the Message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Data
        /// </summary>
        public TEntity? Data { get; set; }

        /// <summary>
        /// Gets or sets the TotalResults
        /// </summary>
        public int TotalResults { get; set; }

        /// <summary>
        /// Gets or sets the Errors
        /// </summary>
        public TError? Errors { get; set; }
    }
}
