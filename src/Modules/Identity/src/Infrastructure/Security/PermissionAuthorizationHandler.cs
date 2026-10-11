using Microsoft.AspNetCore.Authorization;
using Project.Modules.Identity.Application;
using Project.Web.Crud;

namespace Project.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Fail-closed authorization handler. Each endpoint is restricted by the
/// <see cref="PermissionAttribute"/> metadata attached by <c>MapCrud</c>.
/// The current permissions are resolved by the IPermissionService using the
/// JWT's \`tid\` and \`pv\` claims, so changes to role permissions propagate
/// without re-login.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissions;

    public PermissionAuthorizationHandler(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var pvClaim = context.User.FindFirst("pv")?.Value;
        if (!long.TryParse(pvClaim, out var pv))
        {
            return;
        }

        var tid = context.User.FindFirst("tid")?.Value;
        Guid? tenantId = Guid.TryParse(tid, out var parsedTid) ? parsedTid : null;

        var ou = context.User.FindFirst("ou")?.Value;
        Guid? activeUnitId = Guid.TryParse(ou, out var parsedOu) ? parsedOu : null;

        var has = await _permissions.HasAsync(tenantId, userId, pv, activeUnitId, requirement.Key, CancellationToken.None);
        if (has)
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Key { get; }

    public PermissionRequirement(string key)
    {
        Key = key;
    }
}

public static class PermissionRequirementExtensions
{
    public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder builder, string key)
        => builder.AddRequirements(new PermissionRequirement(key));
}