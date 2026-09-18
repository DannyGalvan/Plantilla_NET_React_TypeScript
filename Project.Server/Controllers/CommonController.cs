using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Entities.Response;
using Project.Server.Services.Interfaces;

namespace Project.Server.Controllers
{
    /// <summary>
    /// Base controller exposing the helpers shared by the CRUD stack.
    /// Resolves the current user through <see cref="ICurrentUserService"/> (fail-closed)
    /// and maps <see cref="ResponseStatus"/> onto HTTP status codes (closes B19 + the
    /// "all errors return 400" anti-pattern).
    /// </summary>
    public class CommonController : ControllerBase
    {
        /// <summary>
        /// Lazy-resolves the per-request <see cref="ICurrentUserService"/>.
        /// Fails closed: <see cref="ICurrentUserService.UserId"/> throws when there
        /// is no valid <c>NameIdentifier</c> claim.
        /// </summary>
        protected ICurrentUserService CurrentUser =>
            HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        /// <summary>
        /// Backward-compatible accessor. Returns 0 when no claim is present
        /// so existing controllers that previously expected this behaviour keep
        /// working; new code should call <see cref="CurrentUser"/> and handle the
        /// <see cref="UnauthorizedAccessException"/>.
        /// </summary>
        protected long GetUserId()
        {
            try { return CurrentUser.UserId; }
            catch (UnauthorizedAccessException) { return 0; }
        }

        /// <summary>
        /// Map a <see cref="ResponseStatus"/> to the corresponding HTTP code.
        /// Centralised so every controller agrees on the wire format.
        /// </summary>
        protected static int StatusCodeFor(ResponseStatus status) => status switch
        {
            ResponseStatus.Ok => StatusCodes.Status200OK,
            ResponseStatus.ValidationFailed => StatusCodes.Status400BadRequest,
            ResponseStatus.NotFound => StatusCodes.Status404NotFound,
            ResponseStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResponseStatus.Conflict => StatusCodes.Status409Conflict,
            ResponseStatus.Error => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}