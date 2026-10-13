using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions;
using Project.Application.Organization;
using Project.Application.Security;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Organization.Application;
using Project.Modules.Organization.Contracts;
using Project.Modules.Organization.Domain;
using Project.Modules.Organization.Infrastructure.Persistence;

namespace Project.Modules.Organization.Infrastructure.Services;

/// <summary>Minimal node of the cached tree.</summary>
public sealed record UnitNode(Guid Id, string Path);

/// <summary>Tenant tree and user assignments cached in HybridCache (tag <c>org</c>).</summary>
public sealed class OrganizationTreeCache(IServiceProvider services, HybridCache cache, ITenantContext tenant) : IOrganizationCache
{
    public const string Tag = "org";
    private static readonly HybridCacheEntryOptions Entry = new() { Expiration = TimeSpan.FromMinutes(10), LocalCacheExpiration = TimeSpan.FromMinutes(1) };

    private Guid TenantId => tenant.TenantId ?? TenantDefaults.DefaultTenantId;

    public async Task<IReadOnlyList<UnitNode>> GetTreeAsync(CancellationToken ct) =>
        await cache.GetOrCreateAsync(
            $"org-tree:{TenantId:N}",
            async token => await services.GetRequiredService<OrganizationDbContext>().Units.AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => new UnitNode(u.Id, u.Path))
                .ToListAsync(token),
            Entry,
            [Tag],
            ct);

    public async Task<IReadOnlyList<Guid>> GetAssignedAsync(Guid userId, CancellationToken ct) =>
        await cache.GetOrCreateAsync(
            $"org-assigned:{TenantId:N}:{userId:N}",
            async token => await services.GetRequiredService<OrganizationDbContext>().Assignments.AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(a => a.UnitId)
                .ToListAsync(token),
            Entry,
            [Tag],
            ct);

    public async Task InvalidateAsync(CancellationToken ct) => await cache.RemoveByTagAsync(Tag, ct);

    public static IReadOnlySet<Guid> Descendants(IReadOnlyList<UnitNode> tree, IEnumerable<Guid> roots)
    {
        var prefixes = tree.Where(n => roots.Contains(n.Id)).Select(n => n.Path).ToList();
        return tree.Where(n => prefixes.Any(p => n.Path.StartsWith(p, StringComparison.Ordinal))).Select(n => n.Id).ToHashSet();
    }
}

/// <summary>
/// Organization scope of the request. The active unit comes from the <c>ou</c> claim; accessible units are the active
/// unit and its descendants, or every assigned unit (and descendants) when no unit is selected. Users with
/// <c>organization.scope.all</c> and no selected unit are not filtered. Unauthenticated work (jobs) is not filtered.
/// </summary>
public sealed class OrganizationContext(IServiceProvider services, ICurrentUser user, OrganizationTreeCache tree) : IOrganizationContext
{
    private bool _initialized;

    public bool Enabled => true;

    public Guid? ActiveUnitId { get; private set; }

    public bool HasFullAccess { get; private set; }

    public IReadOnlySet<Guid> AccessibleUnitIds { get; private set; } = new HashSet<Guid>();

    public IReadOnlyList<Guid> ActiveUnitPath { get; private set; } = [];

    public async ValueTask InitializeAsync(CancellationToken ct)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (!user.IsAuthenticated || !Guid.TryParse(user.UserId, out var userId))
        {
            HasFullAccess = true;
            return;
        }

        var nodes = await tree.GetTreeAsync(ct);
        var active = user.ActiveOrganizationUnitId is { } requested ? nodes.FirstOrDefault(n => n.Id == requested) : null;

        // The permission service reads the active path to include roles granted on the unit and its ancestors.
        ActiveUnitPath = active is null ? [] : OrganizationUnit.ParsePath(active.Path);
        var fullAccess = await services.GetRequiredService<IPermissionService>().HasPermissionAsync(OrganizationPermissions.ScopeAll, ct);
        var assigned = fullAccess
            ? nodes.Select(n => n.Id).ToHashSet()
            : OrganizationTreeCache.Descendants(nodes, await tree.GetAssignedAsync(userId, ct));

        if (active is not null && assigned.Contains(active.Id))
        {
            ActiveUnitId = active.Id;
            AccessibleUnitIds = OrganizationTreeCache.Descendants(nodes, [active.Id]);
            HasFullAccess = false;
            return;
        }

        ActiveUnitPath = [];
        AccessibleUnitIds = assigned;
        HasFullAccess = fullAccess;
    }
}

/// <summary>Public contract used by Identity (context switch, role assignments per unit).</summary>
public sealed class OrganizationDirectory(OrganizationTreeCache tree) : IOrganizationDirectory
{
    public async Task<IReadOnlyList<Guid>> GetPathAsync(Guid unitId, CancellationToken ct)
    {
        var node = (await tree.GetTreeAsync(ct)).FirstOrDefault(n => n.Id == unitId);
        return node is null ? [] : OrganizationUnit.ParsePath(node.Path);
    }

    public async Task<IReadOnlyList<Guid>> GetAssignedUnitsAsync(string userId, CancellationToken ct) =>
        Guid.TryParse(userId, out var id) ? await tree.GetAssignedAsync(id, ct) : [];

    public async Task<IReadOnlySet<Guid>> ExpandDescendantsAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken ct) =>
        OrganizationTreeCache.Descendants(await tree.GetTreeAsync(ct), unitIds);
}

/// <summary>Per-tenant seed: default unit types and a root company.</summary>
public sealed class OrganizationSeeder : IDataSeeder
{
    public int Order => 20;

    public SeederScope Scope => SeederScope.PerTenant;

    public async Task SeedAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<OrganizationDbContext>();
        if (!await db.UnitTypes.IgnoreQueryFilters([QueryFilters.SoftDelete]).AnyAsync(ct))
        {
            db.UnitTypes.AddRange(
                new OrganizationUnitType { Code = "company", Name = "Company", CanBeRoot = true, AllowedParentCodes = "company", SortOrder = 10 },
                new OrganizationUnitType { Code = "country", Name = "Country", AllowedParentCodes = "company", SortOrder = 20 },
                new OrganizationUnitType { Code = "brand", Name = "Brand", AllowedParentCodes = "company,country", SortOrder = 30 },
                new OrganizationUnitType { Code = "branch", Name = "Branch", AllowedParentCodes = "company,country,brand", SortOrder = 40 });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Units.IgnoreQueryFilters([QueryFilters.SoftDelete]).AnyAsync(ct))
        {
            var company = await db.UnitTypes.FirstAsync(t => t.Code == "company", ct);
            var root = OrganizationUnit.Create(company, null, "main", "Main company", null).Value;
            db.Units.Add(root);
            await db.SaveChangesAsync(ct);
        }

        await services.GetRequiredService<OrganizationTreeCache>().InvalidateAsync(ct);
    }
}
