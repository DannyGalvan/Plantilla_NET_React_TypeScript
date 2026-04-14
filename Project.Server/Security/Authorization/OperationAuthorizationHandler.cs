using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Project.Server.Security.Authorization
{
    /// <summary>
    /// Defines the <see cref="OperationAuthorizationHandler" />
    /// Handles authorization based on user's allowed operations from JWT claims
    /// </summary>
    public class OperationAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement>
    {
        private readonly ILogger<OperationAuthorizationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationAuthorizationHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger</param>
        public OperationAuthorizationHandler(ILogger<OperationAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Makes a decision if authorization is allowed based on the requirements
        /// </summary>
        /// <param name="context">The authorization context</param>
        /// <param name="requirement">The requirement to evaluate</param>
        /// <returns>The <see cref="Task"/></returns>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement)
        {
            // Get user's OperationKey claims from JWT
            var operationKeyClaims = context.User.FindAll("OperationKey").ToList();

            if (!operationKeyClaims.Any())
            {
                _logger.LogWarning("User {UserId} has no OperationKey claims in JWT", 
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");
                return Task.CompletedTask;
            }

            // Extract operation keys from claims (e.g., "User.Create.POST")
            var userOperationKeys = operationKeyClaims
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Check if user has the required operation key
            if (userOperationKeys.Contains(requirement.OperationKey, StringComparer.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            _logger.LogWarning(
                "Access denied: User {UserId} attempted to access operation {OperationKey} but doesn't have permission. User has: [{Operations}]",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown",
                requirement.OperationKey,
                string.Join(", ", userOperationKeys));

            return Task.CompletedTask;
        }
    }
}
