using Microsoft.EntityFrameworkCore;
using Project.Application.Tenancy;
using Project.Modules.Identity.Contracts;
using Project.Modules.Identity.Infrastructure.Persistence;

namespace Project.Modules.Identity.Infrastructure.Services;

/// <summary>Implementation of the public <see cref="IUserDirectory"/> contract.</summary>
public sealed class UserDirectory(IdentityDbContext db, ITenantContext tenant) : IUserDirectory
{
    private Guid TenantId => tenant.TenantId ?? TenantDefaults.DefaultTenantId;

    public async Task<IReadOnlyList<UserSummary>> FindAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        var tenantId = TenantId;
        return await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id) && db.TenantMemberships.Any(m => m.UserId == u.Id && m.TenantId == tenantId))
            .OrderBy(u => u.UserName)
            .Select(u => new UserSummary(u.Id, u.UserName, u.DisplayName, u.Email))
            .ToListAsync(ct);
    }

    public Task<bool> IsMemberAsync(Guid userId, CancellationToken ct)
    {
        var tenantId = TenantId;
        return db.Users.AnyAsync(u => u.Id == userId && db.TenantMemberships.Any(m => m.UserId == u.Id && m.TenantId == tenantId), ct);
    }

    public Task<IReadOnlyList<UserSummary>> FindWithPermissionAsync(string permission, CancellationToken ct)
    {
        var tenantId = TenantId;
        return UsersOfRolesAsync(db.Roles.Where(r => (r.TenantId == tenantId || r.TenantId == null) && r.Permissions.Any(p => p.PermissionKey == permission)), ct);
    }

    public Task<IReadOnlyList<UserSummary>> FindInRoleAsync(string role, CancellationToken ct)
    {
        var tenantId = TenantId;
        var normalized = role.Trim().ToUpperInvariant();
        return UsersOfRolesAsync(db.Roles.Where(r => (r.TenantId == tenantId || r.TenantId == null) && r.NormalizedName == normalized), ct);
    }

    private async Task<IReadOnlyList<UserSummary>> UsersOfRolesAsync(IQueryable<Domain.Role> roles, CancellationToken ct)
    {
        var tenantId = TenantId;
        var roleIds = roles.Select(r => r.Id);
        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive
                && db.TenantMemberships.Any(m => m.UserId == u.Id && m.TenantId == tenantId)
                && db.RoleAssignments.Any(a => a.UserId == u.Id && (a.TenantId == tenantId || a.TenantId == null) && roleIds.Contains(a.RoleId)))
            .OrderBy(u => u.UserName)
            .Select(u => new UserSummary(u.Id, u.UserName, u.DisplayName, u.Email))
            .ToListAsync(ct);
    }
}
