using FluentValidation.Results;

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
        /// Gets or sets the Errors. Kept separate from Data so a failed response
        /// can carry <see cref="ValidationFailure"/>s without leaking them as
        /// the success payload (closes the original "Errors stored inside Data" pattern).
        /// </summary>
        public TError? Errors { get; set; }

        /// <summary>
        /// Gets or sets the typed status. Drives the HTTP status code returned to
        /// the client and the value of <see cref="Success"/>.
        /// </summary>
        public ResponseStatus Status { get; set; } = ResponseStatus.Ok;
    }
}