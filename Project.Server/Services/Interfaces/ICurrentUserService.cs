namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Resolves the current authenticated user from the active <c>HttpContext</c>.
    /// Fail-closed: if the JWT is missing or malformed, <see cref="UserId"/> throws.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Current user id. Throws <see cref="System.UnauthorizedAccessException"/> when
        /// no valid <c>NameIdentifier</c> claim is present.
        /// </summary>
        long UserId { get; }

        /// <summary>Current user id or null when there is no valid claim.</summary>
        long? UserIdOrNull { get; }

        /// <summary>Rol id from the JWT (0 if missing).</summary>
        long RolId { get; }

        /// <summary>True when an authentication claim is present (does not validate it).</summary>
        bool IsAuthenticated { get; }

        /// <summary>The set of <c>OperationKey</c> claims emitted with the token.</summary>
        IReadOnlySet<string> OperationKeys { get; }

        /// <summary>True when the current user has the given operation key.</summary>
        bool HasOperation(string operationKey);
    }
}