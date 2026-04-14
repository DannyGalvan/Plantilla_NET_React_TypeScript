using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace Project.Server.Security
{
    /// <summary>
    /// Defines the <see cref="SessionAuthorizationFilter" />
    /// </summary>
    /// <remarks>
    /// [OBSOLETE] This filter is no longer used. The application now uses JWT-based authentication
    /// with OperationAuthorizationHandler for granular permission control.
    /// See: Project.Server.Security.Authorization.OperationAuthorizationHandler
    /// </remarks>
    [Obsolete("Use JWT authentication with [Authorize] and [RequireOperation] attributes instead. See OperationAuthorizationHandler.")]
    public class SessionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        /// <summary>
        /// Defines the _logger
        /// </summary>
        private readonly ILogger<SessionAuthorizationFilter> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionAuthorizationFilter"/> class.
        /// </summary>
        public SessionAuthorizationFilter(ILogger<SessionAuthorizationFilter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handles authorization for each request
        /// </summary>
        /// <param name="context">The authorization context</param>
        /// <returns>The <see cref="Task"/></returns>
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Check if endpoint allows anonymous access
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
                return Task.CompletedTask;

            // Check if user is authenticated (session exists)
            var userId = context.HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                return Task.CompletedTask;
            }

            // Get route information
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();
            var method = context.HttpContext.Request.Method?.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
                return Task.CompletedTask;

            // Check if user must change password
            var mustChangePasswordFlag = context.HttpContext.Session.GetString(SessionKeys.MustChangePassword);
            var mustChangePassword = bool.TryParse(mustChangePasswordFlag, out var parsedMustChange) && parsedMustChange;
            var isPasswordFlow = string.Equals(controller, "Auth", StringComparison.OrdinalIgnoreCase)
                                 && (string.Equals(action, "ChangePassword", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(action, "Logout", StringComparison.OrdinalIgnoreCase));

            if (mustChangePassword && !isPasswordFlow)
            {
                context.Result = new RedirectToActionResult("ChangePassword", "Auth", null);
                return Task.CompletedTask;
            }

            // Build operation key
            var operationKey = $"{controller}.{action}.{method}";
            var legacyOperationKey = $"{controller}.{action}";
            var permissionsJson = context.HttpContext.Session.GetString(SessionKeys.AllowedOperations);

            if (string.IsNullOrWhiteSpace(permissionsJson))
            {
                _logger.LogWarning("User {UserId} has no permissions in session", userId.Value);
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return Task.CompletedTask;
            }

            // Check permissions
            var permissions = JsonSerializer.Deserialize<HashSet<string>>(permissionsJson) ?? new HashSet<string>();
            var hasPermission = permissions.Contains(operationKey, StringComparer.OrdinalIgnoreCase)
                             || permissions.Contains(legacyOperationKey, StringComparer.OrdinalIgnoreCase);

            if (!hasPermission)
            {
                _logger.LogWarning("Access denied to operation {OperationKey} for user {UserId}", operationKey, userId.Value);
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
            }

            return Task.CompletedTask;
        }
    }
}
