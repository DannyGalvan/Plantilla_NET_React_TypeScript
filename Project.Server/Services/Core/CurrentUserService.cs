using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Project.Server.Services.Interfaces;

namespace Project.Server.Services.Core
{
    /// <summary>
    /// Resolves the current user from <c>HttpContext.User</c>. Registered as scoped.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _accessor;
        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public CurrentUserService(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public long UserId => UserIdOrNull
            ?? throw new UnauthorizedAccessException("Current user has no NameIdentifier claim.");

        public long? UserIdOrNull
        {
            get
            {
                var raw = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return long.TryParse(raw, out var parsed) ? parsed : null;
            }
        }

        public long RolId
        {
            get
            {
                var raw = Principal?.FindFirst("RolId")?.Value;
                return long.TryParse(raw, out var parsed) ? parsed : 0L;
            }
        }

        public IReadOnlySet<string> OperationKeys
        {
            get
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (Principal == null) return set;
                foreach (var claim in Principal.FindAll("OperationKey"))
                {
                    if (!string.IsNullOrWhiteSpace(claim.Value)) set.Add(claim.Value);
                }
                return set;
            }
        }

        public bool HasOperation(string operationKey)
            => !string.IsNullOrWhiteSpace(operationKey) && OperationKeys.Contains(operationKey);
    }
}