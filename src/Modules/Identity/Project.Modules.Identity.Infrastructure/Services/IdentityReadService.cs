using Microsoft.EntityFrameworkCore;
using Project.Application;
using Project.Application.Querying;
using Project.Modules.Identity.Application;
using Project.Modules.Identity.Domain;
using Project.Modules.Identity.Infrastructure.Persistence;
using Project.SharedKernel;

namespace Project.Modules.Identity.Infrastructure.Services;

public sealed class UserQueryPolicy : QueryPolicy<User>
{
    public UserQueryPolicy()
    {
        Field(u => u.UserName).Field(u => u.Email).Field(u => u.DisplayName).Field(u => u.IsActive).Field(u => u.CreatedAt).Field(u => u.LastLoginAt);
        DefaultOrder(u => u.UserName);
    }
}

public sealed class RoleQueryPolicy : QueryPolicy<Role>
{
    public RoleQueryPolicy()
    {
        Field(r => r.Name).Field(r => r.IsSystem).Field(r => r.CreatedAt);
        DefaultOrder(r => r.Name);
    }
}

/// <summary>EF Core read side of Identity: projections scoped to the tenant.</summary>
public sealed class IdentityReadService(
    IdentityDbContext db,
    QueryParser parser,
    IQueryPolicy<User> userPolicy,
    IQueryPolicy<Role> rolePolicy,
    TimeProvider clock) : IIdentityReadService
{
    public async Task<Result<PagedResult<UserDto>>> ListUsersAsync(PageRequest page, Guid tenantId, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().Where(u => db.TenantMemberships.Any(m => m.UserId == u.Id && m.TenantId == tenantId));
        var shaped = Shape(query, page, parser, userPolicy, u => u.Id);
        if (shaped.IsFailure)
        {
            return shaped.Error!;
        }

        var (filtered, ordered) = shaped.Value;
        var size = page.NormalizedPageSize(PaginationDefaults.MaxPageSize);
        long? total = page.IncludeTotal ? await filtered.LongCountAsync(ct) : null;
        var users = await ordered.Skip((page.NormalizedPage - 1) * size).Take(size).ToListAsync(ct);
        var items = await ToDtosAsync(users, tenantId, ct);
        return new PagedResult<UserDto>(items, page.NormalizedPage, size, total);
    }

    public async Task<UserDto?> GetUserAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == id && db.TenantMemberships.Any(m => m.UserId == u.Id && m.TenantId == tenantId))
            .FirstOrDefaultAsync(ct);
        return user is null ? null : (await ToDtosAsync([user], tenantId, ct))[0];
    }

    public async Task<Result<PagedResult<RoleDto>>> ListRolesAsync(PageRequest page, Guid tenantId, CancellationToken ct)
    {
        var query = db.Roles.AsNoTracking().Where(r => r.TenantId == tenantId || r.TenantId == null);
        var shaped = Shape(query, page, parser, rolePolicy, r => r.Id);
        if (shaped.IsFailure)
        {
            return shaped.Error!;
        }

        var (filtered, ordered) = shaped.Value;
        var size = page.NormalizedPageSize(PaginationDefaults.MaxPageSize);
        long? total = page.IncludeTotal ? await filtered.LongCountAsync(ct) : null;
        var roles = await ordered.Include(r => r.Permissions).Skip((page.NormalizedPage - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<RoleDto>(await ToRoleDtosAsync(roles, tenantId, ct), page.NormalizedPage, size, total);
    }

    public async Task<RoleDto?> GetRoleAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id && (r.TenantId == tenantId || r.TenantId == null), ct);
        return role is null ? null : (await ToRoleDtosAsync([role], tenantId, ct))[0];
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, Guid tenantId, CancellationToken ct) =>
        await db.RoleAssignments.AsNoTracking()
            .Where(a => a.UserId == userId && (a.TenantId == tenantId || a.TenantId == null))
            .Join(db.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);

    public Task<bool> IsPlatformAdminAsync(Guid userId, CancellationToken ct) =>
        db.RoleAssignments.AsNoTracking()
            .Where(a => a.UserId == userId && a.TenantId == null)
            .Join(db.Roles, a => a.RoleId, r => r.Id, (a, r) => r)
            .AnyAsync(r => r.TenantId == null && r.IsSystem, ct);

    private async Task<List<UserDto>> ToDtosAsync(IReadOnlyList<User> users, Guid tenantId, CancellationToken ct)
    {
        var ids = users.Select(u => u.Id).ToList();
        var assignments = await db.RoleAssignments.AsNoTracking()
            .Where(a => ids.Contains(a.UserId) && a.TenantId == tenantId)
            .Join(db.Roles, a => a.RoleId, r => r.Id, (a, r) => new { a.Id, a.UserId, a.RoleId, r.Name, a.OrganizationUnitId })
            .ToListAsync(ct);

        var now = clock.GetUtcNow();
        return users.Select(u =>
        {
            var own = assignments.Where(a => a.UserId == u.Id).ToList();
            return new UserDto(
                u.Id,
                u.UserName,
                u.Email,
                u.DisplayName,
                u.IsActive,
                u.IsLockedOut(now),
                u.MustChangePassword,
                u.LastLoginAt,
                u.CreatedAt,
                own.Where(a => a.OrganizationUnitId == null).Select(a => new RoleRefDto(a.RoleId, a.Name)).OrderBy(r => r.Name).ToList(),
                own.Select(a => new RoleAssignmentDto(a.Id, a.RoleId, a.Name, a.OrganizationUnitId)).ToList(),
                u.ConcurrencyStamp);
        }).ToList();
    }

    private async Task<List<RoleDto>> ToRoleDtosAsync(IReadOnlyList<Role> roles, Guid tenantId, CancellationToken ct)
    {
        var ids = roles.Select(r => r.Id).ToList();
        var counts = await db.RoleAssignments.AsNoTracking()
            .Where(a => ids.Contains(a.RoleId) && (a.TenantId == tenantId || a.TenantId == null))
            .GroupBy(a => a.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Select(a => a.UserId).Distinct().Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);

        return roles.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.Description,
            r.IsSystem,
            r.TenantId is null,
            r.Permissions.Select(p => p.PermissionKey).Order().ToList(),
            counts.GetValueOrDefault(r.Id),
            r.CreatedAt,
            r.ConcurrencyStamp)).ToList();
    }

    private static Result<(IQueryable<T> Filtered, IQueryable<T> Ordered)> Shape<T>(
        IQueryable<T> query,
        PageRequest page,
        QueryParser parser,
        IQueryPolicy<T> policy,
        System.Linq.Expressions.Expression<Func<T, Guid>> id)
        where T : class
    {
        var filter = parser.ParseFilter(page.Filter, policy);
        if (filter.IsFailure)
        {
            return filter.Error!;
        }

        var sort = QueryParser.ParseSort(page.Sort, policy);
        if (sort.IsFailure)
        {
            return sort.Error!;
        }

        var filtered = filter.Value is null ? query : query.Where(filter.Value);
        var spec = new Project.Application.Abstractions.Specification<T>();
        foreach (var (selector, descending) in sort.Value)
        {
            spec.OrderBy(selector, descending);
        }

        spec.OrderBy(id);
        var ordered = Project.Infrastructure.Persistence.SpecificationEvaluator.Apply(filtered, spec, applyPaging: false);
        return (filtered, ordered);
    }
}
