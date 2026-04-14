using Microsoft.AspNetCore.Authorization;

namespace Project.Server.Security.Authorization
{
    /// <summary>
    /// Defines the <see cref="OperationAuthorizationRequirement" />
    /// Requirement that validates if user has permission for a specific operation
    /// </summary>
    public class OperationAuthorizationRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Gets the operation key required (e.g., "User.Create.POST")
        /// </summary>
        public string OperationKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationAuthorizationRequirement"/> class.
        /// </summary>
        /// <param name="operationKey">The operation key required</param>
        public OperationAuthorizationRequirement(string operationKey)
        {
            OperationKey = operationKey;
        }
    }
}
