using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Project.Application.Abstractions;
using Project.Application.Organization;
using Project.Application.Security;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Identity.Application;
using Project.Modules.Identity.Application.Roles;
using Project.Modules.Identity.Contracts;
using Project.Modules.Identity.Domain;
using Project.Modules.Identity.Infrastructure.Persistence;

namespace Project.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Effective permissions: union of the roles assigned platform-wide, tenant-wide and on the active organization unit
/// or its ancestors. Cached in HybridCache per user, tenant and unit, tagged <c>user:{id}</c> for invalidation.
/// </summary>
public sealed class PermissionService(
    IdentityDbContext db,
    HybridCache cache,
    ICurrentUser currentUser,
    ITenantContext tenant,
    IOrganizationContext organization) : IPermissionService, IPermissionCache
{
    private IReadOnlySet<string>? _resolved;

    public const string AllTag = "perm-all";

    public static string UserTag(Guid userId) => $"perm-user:{userId:N}";

    public async ValueTask<IReadOnlySet<string>> GetPermissionsAsync(CancellationToken ct)
    {
        if (_resolved is not null)
        {
            return _resolved;
        }

        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            return _resolved = new HashSet<string>();
        }

        var tenantId = tenant.TenantId ?? TenantDefaults.DefaultTenantId;
        var path = organization.Enabled ? organization.ActiveUnitPath : [];
        var key = $"perm:{userId:N}:{tenantId:N}:{(path.Count == 0 ? "-" : path[^1].ToString("N"))}";

        var permissions = await cache.GetOrCreateAsync(
            key,
            async token => await LoadAsync(userId, tenantId, path, token),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(30), LocalCacheExpiration = TimeSpan.FromMinutes(5) },
            [UserTag(userId), AllTag],
            ct);

        return _resolved = permissions.ToHashSet(StringComparer.Ordinal);
    }

    public async ValueTask<bool> HasPermissionAsync(string permission, CancellationToken ct) =>
        (await GetPermissionsAsync(ct)).Contains(permission);

    public async Task InvalidateUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        _resolved = null;
        foreach (var userId in userIds.Distinct())
        {
            await cache.RemoveByTagAsync(UserTag(userId), ct);
        }
    }

    public async Task InvalidateRoleAsync(Guid roleId, CancellationToken ct)
    {
        var users = await db.RoleAssignments.AsNoTracking().Where(a => a.RoleId == roleId).Select(a => a.UserId).Distinct().ToListAsync(ct);
        if (users.Count > 0)
        {
            await db.Users.Where(u => users.Contains(u.Id)).ExecuteUpdateAsync(s => s.SetProperty(u => u.PermissionsVersion, u => u.PermissionsVersion + 1), ct);
        }

        await InvalidateUsersAsync(users, ct);
    }

    private async Task<string[]> LoadAsync(Guid userId, Guid tenantId, IReadOnlyList<Guid> unitPath, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId && u.IsActive, ct))
        {
            return [];
        }

        var units = unitPath.ToList();
        var roleIds = await db.RoleAssignments.AsNoTracking()
            .Where(a => a.UserId == userId
                && (a.TenantId == null || a.TenantId == tenantId)
                && (a.OrganizationUnitId == null || units.Contains(a.OrganizationUnitId.Value)))
            .Select(a => a.RoleId)
            .Distinct()
            .ToListAsync(ct);

        if (roleIds.Count == 0)
        {
            return [];
        }

        var roles = await db.Roles.AsNoTracking().Include(r => r.Permissions).Where(r => roleIds.Contains(r.Id)).ToListAsync(ct);
        return roles.SelectMany(r => r.Permissions.Select(p => p.PermissionKey)).Distinct().ToArray();
    }
}

/// <summary>
/// Upserts the permissions declared by every module and grants all of them to the SuperAdmin platform role.
/// Permissions that are no longer declared are removed from the catalog and from every role.
/// </summary>
public sealed class PermissionSynchronizer(
    IdentityDbContext db,
    PermissionCatalog catalog,
    HybridCache cache,
    ILogger<PermissionSynchronizer> logger) : IPermissionSynchronizer
{
    public async Task SynchronizeAsync(CancellationToken ct)
    {
        var declared = catalog.All.ToDictionary(p => p.Key);
        var stored = await db.Permissions.ToListAsync(ct);

        foreach (var permission in stored)
        {
            if (declared.TryGetValue(permission.Id, out var definition))
            {
                permission.Update(definition.Module, definition.DisplayKey, definition.Group);
            }
            else
            {
                db.Permissions.Remove(permission);
            }
        }

        foreach (var definition in declared.Values.Where(d => stored.All(s => s.Id != d.Key)))
        {
            db.Permissions.Add(Permission.Create(definition.Key, definition.Module, definition.DisplayKey, definition.Group));
        }

        var roles = await db.Roles.IgnoreQueryFilters([QueryFilters.SoftDelete]).Include(r => r.Permissions).ToListAsync(ct);
        foreach (var role in roles)
        {
            if (role.IsSystem && role.TenantId is null && role.Name == IdentityRoles.SuperAdmin)
            {
                role.SetPermissions(declared.Keys);
            }
            else
            {
                role.SetPermissions(role.Permissions.Select(p => p.PermissionKey).Where(declared.ContainsKey));
            }
        }

        await db.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(PermissionService.AllTag, ct);
        logger.LogInformation("{Count} permissions declared by the modules", declared.Count);
    }
}

/// <summary>
/// Platform seed: the SuperAdmin role and the first administrator (password from <c>Seed:AdminPassword</c>,
/// mandatory outside Development, must be changed at the first sign-in).
/// </summary>
public sealed class IdentityPlatformSeeder(
    IConfiguration configuration,
    IHostEnvironment environment,
    IPasswordHasher hasher,
    TimeProvider clock,
    ILogger<IdentityPlatformSeeder> logger) : IDataSeeder
{
    public int Order => 10;

    public SeederScope Scope => SeederScope.Platform;

    public async Task SeedAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<IdentityDbContext>();
        var now = clock.GetUtcNow();

        var superAdmin = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == null && r.Name == IdentityRoles.SuperAdmin, ct);
        if (superAdmin is null)
        {
            superAdmin = Role.Create(IdentityRoles.SuperAdmin, "Every permission in every tenant.", tenantId: null, isSystem: true);
            db.Roles.Add(superAdmin);
        }

        if (await db.Users.IgnoreQueryFilters([QueryFilters.SoftDelete]).AnyAsync(ct))
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        var userName = configuration["Seed:AdminUserName"] ?? "admin";
        var email = configuration["Seed:AdminEmail"] ?? "admin@example.com";
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Seed:AdminPassword is required to create the first administrator outside Development.");
            }

            password = $"Dev-{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(6))}!a";
            logger.LogWarning(
                "Seed:AdminPassword is not configured. Generated development password for '{UserName}': {Password} (change it at the first sign-in)",
                userName,
                password);
        }

        var admin = User.Create(userName, email, "Administrator", hasher.Hash(password), mustChangePassword: true, now);
        db.Users.Add(admin);
        db.TenantMemberships.Add(TenantMembership.Create(admin.Id, TenantDefaults.DefaultTenantId, isDefault: true, now));
        db.RoleAssignments.Add(RoleAssignment.Create(admin.Id, superAdmin.Id, tenantId: null, organizationUnitId: null));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Administrator '{UserName}' created", userName);
    }
}

/// <summary>Per-tenant seed: the Administrator and User roles of each tenant.</summary>
public sealed class IdentityTenantSeeder(PermissionCatalog catalog) : IDataSeeder
{
    public int Order => 10;

    public SeederScope Scope => SeederScope.PerTenant;

    public async Task SeedAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<IdentityDbContext>();
        var tenantId = services.GetRequiredService<ITenantContext>().TenantId ?? TenantDefaults.DefaultTenantId;
        var assignable = catalog.Assignable.Select(p => p.Key).ToList();

        await EnsureRoleAsync(db, tenantId, IdentityRoles.Administrator, "Full access to the tenant.", assignable, ct);
        await EnsureRoleAsync(db, tenantId, IdentityRoles.User, "Read access to business data.",
            assignable.Where(k => k.EndsWith(".read", StringComparison.Ordinal) && !k.StartsWith("identity.", StringComparison.Ordinal) && !k.StartsWith("tenancy.", StringComparison.Ordinal)),
            ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureRoleAsync(IdentityDbContext db, Guid tenantId, string name, string description, IEnumerable<string> permissions, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == name, ct);
        if (role is null)
        {
            role = Role.Create(name, description, tenantId);
            role.SetPermissions(permissions);
            db.Roles.Add(role);
        }
        else if (name == IdentityRoles.Administrator)
        {
            // Administrators always get the permissions of newly added modules.
            role.SetPermissions(role.Permissions.Select(p => p.PermissionKey).Union(permissions));
        }
    }
}

/// <summary>Invalidates the permission cache when a role changes (dispatched from the outbox).</summary>
public sealed class RolePermissionsChangedHandler(IPermissionCache cache) : IDomainEventHandler<RolePermissionsChanged>
{
    public Task HandleAsync(RolePermissionsChanged domainEvent, CancellationToken ct) => cache.InvalidateRoleAsync(domainEvent.RoleId, ct);
}
