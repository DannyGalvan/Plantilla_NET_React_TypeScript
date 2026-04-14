using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Project.Server.Security.Authorization
{
    /// <summary>
    /// Defines the <see cref="RequireOperationAttribute" />
    /// Authorization attribute that validates if user has permission for the current operation
    /// based on Controller.Action.HttpMethod pattern
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireOperationAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequireOperationAttribute"/> class.
        /// Automatically constructs the operation key from the route context
        /// </summary>
        public RequireOperationAttribute() : base(typeof(RequireOperationFilter))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireOperationAttribute"/> class.
        /// </summary>
        /// <param name="operationKey">Explicit operation key (e.g., "User.Create.POST")</param>
        public RequireOperationAttribute(string operationKey) : base(typeof(RequireOperationFilter))
        {
            Arguments = new object[] { operationKey };
        }

        /// <summary>
        /// Filter that performs the actual authorization check
        /// </summary>
        private class RequireOperationFilter : IAsyncAuthorizationFilter
        {
            private readonly IAuthorizationService _authorizationService;
            private readonly ILogger<RequireOperationFilter> _logger;
            private readonly string? _explicitOperationKey;

            public RequireOperationFilter(
                IAuthorizationService authorizationService,
                ILogger<RequireOperationFilter> logger,
                string? explicitOperationKey = null)
            {
                _authorizationService = authorizationService;
                _logger = logger;
                _explicitOperationKey = explicitOperationKey;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                // Check if endpoint allows anonymous access
                var endpoint = context.HttpContext.GetEndpoint();
                if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
                    return;

                // Get route information
                var controller = context.RouteData.Values["controller"]?.ToString();
                var action = context.RouteData.Values["action"]?.ToString();
                var method = context.HttpContext.Request.Method?.ToUpperInvariant();

                // Build operation key: Controller.Action.HttpMethod
                var operationKey = _explicitOperationKey ?? $"{controller}.{action}.{method}";

                if (string.IsNullOrWhiteSpace(operationKey))
                {
                    _logger.LogError("Could not determine operation key for authorization");
                    context.Result = new ForbidResult();
                    return;
                }

                // Create requirement with the operation key
                var requirement = new OperationAuthorizationRequirement(operationKey);

                // Authorize
                var authResult = await _authorizationService.AuthorizeAsync(
                    context.HttpContext.User,
                    null,
                    requirement);

                if (!authResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Authorization failed for operation {OperationKey}. User: {UserId}",
                        operationKey,
                        context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Unknown");

                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = "No tiene permisos para realizar esta operación",
                        errors = new[] { "Acceso no autorizado" }
                    })
                    {
                        StatusCode = 403
                    };
                }
            }
        }
    }
}
