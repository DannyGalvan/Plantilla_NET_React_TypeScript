namespace Project.Server.Entities.Response
{
    /// <summary>
    /// Status codes for <see cref="Response{TEntity, TError}"/>.
    /// Maps to HTTP status codes via <c>CommonController.ToHttpStatus</c>.
    /// </summary>
    public enum ResponseStatus
    {
        Ok = 0,
        ValidationFailed,
        NotFound,
        Forbidden,
        Conflict,
        Error
    }
}