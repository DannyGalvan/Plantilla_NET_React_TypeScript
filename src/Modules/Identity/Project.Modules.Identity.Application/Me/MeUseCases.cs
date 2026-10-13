using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Modules.Identity.Domain;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application.Me;

public sealed record TenantSummaryDto(Guid Id, string Code, string Name, bool IsDefault);

/// <summary>Profile of the signed-in user in the current context, including effective permissions.</summary>
public sealed record ProfileDto(
    Guid Id,
    string UserName,
    string Email,
    string? DisplayName,
    bool MustChangePassword,
    TenantSummaryDto? Tenant,
    Guid? OrganizationUnitId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<TenantSummaryDto> Tenants,
    long PermissionsVersion,
    bool IsPlatformAdmin);

public sealed record GetProfileQuery : IQuery<ProfileDto>;

public sealed class GetProfileHandler(
    IRepository<User, Guid> users,
    IRepository<TenantMembership, Guid> memberships,
    IIdentityReadService reads,
    IPermissionService permissions,
    ITenantContext tenant,
    ITenantStore tenantStore,
    ICurrentUser currentUser) : IQueryHandler<GetProfileQuery, ProfileDto>
{
    public async ValueTask<Result<ProfileDto>> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
        {
            return Error.Unauthorized("auth.required", "Authentication is required.");
        }

        var user = await users.FirstOrDefaultAsync(new Specification<User>().Where(u => u.Id == userId), cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized("auth.required", "Authentication is required.");
        }

        var tenantId = tenant.TenantId ?? TenantDefaults.DefaultTenantId;
        var isPlatformAdmin = await reads.IsPlatformAdminAsync(userId, cancellationToken);
        var userMemberships = await memberships.ListAsync(new Specification<TenantMembership>().Where(m => m.UserId == userId), cancellationToken);

        var tenants = new List<TenantSummaryDto>();
        foreach (var membership in userMemberships)
        {
            if (await tenantStore.FindByIdAsync(membership.TenantId, cancellationToken) is { Status: TenantStatus.Active } info)
            {
                tenants.Add(new TenantSummaryDto(info.Id, info.Code, info.Name, membership.IsDefault));
            }
        }

        if (isPlatformAdmin && tenants.Count == 0)
        {
            tenants.AddRange((await tenantStore.ListActiveAsync(cancellationToken)).Select(t => new TenantSummaryDto(t.Id, t.Code, t.Name, t.Id == TenantDefaults.DefaultTenantId)));
        }

        var current = tenant.Tenant;
        return new ProfileDto(
            user.Id,
            user.UserName,
            user.Email,
            user.DisplayName,
            user.MustChangePassword,
            current is null ? null : new TenantSummaryDto(current.Id, current.Code, current.Name, tenants.Any(t => t.Id == current.Id && t.IsDefault)),
            currentUser.ActiveOrganizationUnitId,
            await reads.GetRoleNamesAsync(userId, tenantId, cancellationToken),
            (await permissions.GetPermissionsAsync(cancellationToken)).Order().ToList(),
            tenants.OrderBy(t => t.Name).ToList(),
            user.PermissionsVersion,
            isPlatformAdmin);
    }
}

public sealed record NavigationItemDto(string Key, string Module, string LabelKey, string Path, string Icon, int Order);

/// <summary>Menu entries the user may see: declared by the modules, filtered by permission and feature.</summary>
public sealed record GetNavigationQuery : IQuery<IReadOnlyList<NavigationItemDto>>;

public sealed class GetNavigationHandler(
    IEnumerable<IPermissionProvider> providers,
    IPermissionService permissions,
    IFeatureFlags features) : IQueryHandler<GetNavigationQuery, IReadOnlyList<NavigationItemDto>>
{
    public async ValueTask<Result<IReadOnlyList<NavigationItemDto>>> Handle(GetNavigationQuery query, CancellationToken cancellationToken)
    {
        var granted = await permissions.GetPermissionsAsync(cancellationToken);
        var enabled = (await features.GetEnabledAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<NavigationItemDto> items = providers
            .SelectMany(p => p.GetNavigation())
            .Where(n => n.Permission is null || granted.Contains(n.Permission))
            .Where(n => n.Feature is null || enabled.Contains(n.Feature))
            .OrderBy(n => n.Order)
            .Select(n => new NavigationItemDto(n.Key, n.Module, n.LabelKey, n.Path, n.Icon, n.Order))
            .ToList();
        return Result<IReadOnlyList<NavigationItemDto>>.Success(items);
    }
}
