using FluentValidation;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Application.Tenancy;
using Project.Modules.Identity.Contracts;
using Project.Modules.Identity.Domain;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application.Roles;

/// <summary>Roles visible in a tenant: its own roles and the shared platform roles (read-only).</summary>
public sealed class RoleScope(IRepository<Role, Guid> roles, ITenantContext tenant)
{
    public Guid TenantId => tenant.TenantId ?? TenantDefaults.DefaultTenantId;

    /// <summary>Loads a role the tenant owns. Platform roles cannot be changed from a tenant.</summary>
    public async Task<Result<Role>> FindEditableAsync(Guid id, bool includeDeleted, CancellationToken ct)
    {
        var tenantId = TenantId;
        var spec = new Specification<Role>().Where(r => r.Id == id && (r.TenantId == tenantId || r.TenantId == null)).Include(nameof(Role.Permissions)).Tracked();
        var role = await roles.FirstOrDefaultAsync(includeDeleted ? spec.WithDeleted() : spec, ct);
        if (role is null)
        {
            return Error.NotFound("role.not_found");
        }

        return role.TenantId is null || role.IsSystem
            ? Error.Conflict("role.read_only", "System and platform roles cannot be modified.")
            : role;
    }

    public Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken ct)
    {
        var normalized = User.Normalize(name);
        var tenantId = TenantId;
        return roles.AnyAsync(
            new Specification<Role>().Where(r => r.NormalizedName == normalized && (r.TenantId == tenantId || r.TenantId == null) && r.Id != exceptId).WithDeleted(),
            ct);
    }
}

public sealed record ListRolesQuery(PageRequest Page) : IQuery<PagedResult<RoleDto>>;

public sealed class ListRolesHandler(IIdentityReadService reads, RoleScope scope) : IQueryHandler<ListRolesQuery, PagedResult<RoleDto>>
{
    public async ValueTask<Result<PagedResult<RoleDto>>> Handle(ListRolesQuery query, CancellationToken cancellationToken) =>
        await reads.ListRolesAsync(query.Page, scope.TenantId, cancellationToken);
}

public sealed record GetRoleQuery(Guid Id) : IQuery<RoleDto>;

public sealed class GetRoleHandler(IIdentityReadService reads, RoleScope scope) : IQueryHandler<GetRoleQuery, RoleDto>
{
    public async ValueTask<Result<RoleDto>> Handle(GetRoleQuery query, CancellationToken cancellationToken) =>
        await reads.GetRoleAsync(query.Id, scope.TenantId, cancellationToken) is { } role ? role : Error.NotFound("role.not_found");
}

public sealed record CreateRoleCommand(string Name, string? Description, IReadOnlyList<string>? Permissions) : ICommand<RoleDto>;

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(64);
        RuleFor(c => c.Description).MaximumLength(256);
    }
}

public sealed class CreateRoleHandler(
    IRepository<Role, Guid> roles,
    RoleScope scope,
    PermissionCatalog catalog,
    IIdentityReadService reads) : ICommandHandler<CreateRoleCommand, RoleDto>
{
    public async ValueTask<Result<RoleDto>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        if (await scope.NameTakenAsync(command.Name, null, cancellationToken))
        {
            return Error.Conflict("role.name_taken", "A role with this name already exists.");
        }

        var permissions = catalog.ValidateAssignable(command.Permissions ?? []);
        if (permissions.IsFailure)
        {
            return permissions.Error!;
        }

        var role = Role.Create(command.Name, command.Description, scope.TenantId);
        role.SetPermissions(permissions.Value);
        roles.Add(role);
        await roles.UnitOfWork.SaveChangesAsync(cancellationToken);
        return (await reads.GetRoleAsync(role.Id, scope.TenantId, cancellationToken))!;
    }
}

public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description, Guid? ConcurrencyStamp) : ICommand<RoleDto>;

public sealed class UpdateRoleValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(64);
        RuleFor(c => c.Description).MaximumLength(256);
    }
}

public sealed class UpdateRoleHandler(IRepository<Role, Guid> roles, RoleScope scope, IIdentityReadService reads)
    : ICommandHandler<UpdateRoleCommand, RoleDto>
{
    public async ValueTask<Result<RoleDto>> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await scope.FindEditableAsync(command.Id, includeDeleted: false, cancellationToken);
        if (role.IsFailure)
        {
            return role.Error!;
        }

        if (command.ConcurrencyStamp is { } stamp && stamp != role.Value.ConcurrencyStamp)
        {
            return Error.Conflict("concurrency.conflict", "The role was modified by someone else. Reload it and try again.");
        }

        if (await scope.NameTakenAsync(command.Name, role.Value.Id, cancellationToken))
        {
            return Error.Conflict("role.name_taken", "A role with this name already exists.");
        }

        role.Value.Update(command.Name, command.Description);
        await roles.UnitOfWork.SaveChangesAsync(cancellationToken);
        return (await reads.GetRoleAsync(role.Value.Id, scope.TenantId, cancellationToken))!;
    }
}

public sealed record DeleteRoleCommand(Guid Id) : ICommand;

public sealed class DeleteRoleHandler(
    IRepository<Role, Guid> roles,
    IRepository<RoleAssignment, Guid> assignments,
    RoleScope scope,
    IPermissionCache permissionCache) : ICommandHandler<DeleteRoleCommand>
{
    public async ValueTask<Result> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await scope.FindEditableAsync(command.Id, includeDeleted: false, cancellationToken);
        if (role.IsFailure)
        {
            return role.Error!;
        }

        if (await assignments.AnyAsync(new Specification<RoleAssignment>().Where(a => a.RoleId == command.Id), cancellationToken))
        {
            return Error.Conflict("role.in_use", "The role is assigned to users. Remove the assignments first.");
        }

        roles.Remove(role.Value);
        await roles.UnitOfWork.SaveChangesAsync(cancellationToken);
        await permissionCache.InvalidateRoleAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record RestoreRoleCommand(Guid Id) : ICommand;

public sealed class RestoreRoleHandler(IRepository<Role, Guid> roles, RoleScope scope) : ICommandHandler<RestoreRoleCommand>
{
    public async ValueTask<Result> Handle(RestoreRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await scope.FindEditableAsync(command.Id, includeDeleted: true, cancellationToken);
        if (role.IsFailure || !role.Value.IsDeleted)
        {
            return Error.NotFound("role.not_found");
        }

        role.Value.IsDeleted = false;
        role.Value.DeletedAt = null;
        role.Value.DeletedBy = null;
        await roles.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Replaces the permissions of a role. Users of the role see the change on their next request.</summary>
public sealed record SetRolePermissionsCommand(Guid Id, IReadOnlyList<string> Permissions) : ICommand<RoleDto>;

public sealed class SetRolePermissionsHandler(
    IRepository<Role, Guid> roles,
    RoleScope scope,
    PermissionCatalog catalog,
    IPermissionCache permissionCache,
    IIdentityReadService reads) : ICommandHandler<SetRolePermissionsCommand, RoleDto>
{
    public async ValueTask<Result<RoleDto>> Handle(SetRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        var role = await scope.FindEditableAsync(command.Id, includeDeleted: false, cancellationToken);
        if (role.IsFailure)
        {
            return role.Error!;
        }

        var permissions = catalog.ValidateAssignable(command.Permissions);
        if (permissions.IsFailure)
        {
            return permissions.Error!;
        }

        if (role.Value.SetPermissions(permissions.Value))
        {
            await roles.UnitOfWork.SaveChangesAsync(cancellationToken);
            await permissionCache.InvalidateRoleAsync(role.Value.Id, cancellationToken);
        }

        return (await reads.GetRoleAsync(role.Value.Id, scope.TenantId, cancellationToken))!;
    }
}

public sealed record PermissionGroupDto(string Module, IReadOnlyList<PermissionDefinition> Permissions);

/// <summary>Permissions declared by every loaded module, grouped by module, for the role editor.</summary>
public sealed record ListPermissionsQuery : IQuery<IReadOnlyList<PermissionGroupDto>>;

public sealed class ListPermissionsHandler(PermissionCatalog catalog) : IQueryHandler<ListPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    public ValueTask<Result<IReadOnlyList<PermissionGroupDto>>> Handle(ListPermissionsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<PermissionGroupDto> groups = catalog.Assignable
            .GroupBy(p => p.Module)
            .OrderBy(g => g.Key)
            .Select(g => new PermissionGroupDto(g.Key, g.OrderBy(p => p.Key).ToList()))
            .ToList();
        return ValueTask.FromResult(Result<IReadOnlyList<PermissionGroupDto>>.Success(groups));
    }
}

/// <summary>
/// Permissions declared by the modules. Platform administration permissions (tenant catalog) are reserved to the
/// SuperAdmin platform role and cannot be granted to tenant roles.
/// </summary>
public sealed class PermissionCatalog(IEnumerable<IPermissionProvider> providers)
{
    private static readonly string[] PlatformOnlyPrefixes = ["tenancy.tenants."];

    public IReadOnlyList<PermissionDefinition> All { get; } = providers.SelectMany(p => p.GetPermissions()).DistinctBy(p => p.Key).ToList();

    public IEnumerable<PermissionDefinition> Assignable => All.Where(p => !IsReserved(p.Key));

    public static bool IsPlatformOnly(string key) => PlatformOnlyPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>Platform permissions: by legacy prefix or declared with <c>PlatformOnly</c>.</summary>
    public bool IsReserved(string key) => IsPlatformOnly(key) || All.Any(p => p.Key == key && p.PlatformOnly);

    public Result<IReadOnlyList<string>> ValidateAssignable(IEnumerable<string> keys)
    {
        var normalized = keys.Select(k => k.Trim().ToLowerInvariant()).Distinct().ToList();
        var unknown = normalized.Where(k => IsReserved(k) || All.All(p => p.Key != k)).ToList();
        return unknown.Count == 0
            ? Result<IReadOnlyList<string>>.Success(normalized)
            : Error.Validation("permissions", $"Unknown or reserved permissions: {string.Join(", ", unknown)}.");
    }
}

/// <summary>Permissions and navigation of the Identity module.</summary>
public sealed class IdentityPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(IdentityPermissions.Users.Read, IdentityPermissions.Module, "permissions.identity.users.read", "users"),
        new(IdentityPermissions.Users.Create, IdentityPermissions.Module, "permissions.identity.users.create", "users"),
        new(IdentityPermissions.Users.Update, IdentityPermissions.Module, "permissions.identity.users.update", "users"),
        new(IdentityPermissions.Users.Delete, IdentityPermissions.Module, "permissions.identity.users.delete", "users"),
        new(IdentityPermissions.Users.Restore, IdentityPermissions.Module, "permissions.identity.users.restore", "users"),
        new(IdentityPermissions.Users.ResetPassword, IdentityPermissions.Module, "permissions.identity.users.reset-password", "users"),
        new(IdentityPermissions.Roles.Read, IdentityPermissions.Module, "permissions.identity.roles.read", "roles"),
        new(IdentityPermissions.Roles.Create, IdentityPermissions.Module, "permissions.identity.roles.create", "roles"),
        new(IdentityPermissions.Roles.Update, IdentityPermissions.Module, "permissions.identity.roles.update", "roles"),
        new(IdentityPermissions.Roles.Delete, IdentityPermissions.Module, "permissions.identity.roles.delete", "roles"),
        new(IdentityPermissions.Roles.Restore, IdentityPermissions.Module, "permissions.identity.roles.restore", "roles"),
        new(IdentityPermissions.Roles.AssignPermissions, IdentityPermissions.Module, "permissions.identity.roles.assign-permissions", "roles"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("identity.users", IdentityPermissions.Module, "nav.users", "/identity/users", "users", IdentityPermissions.Users.Read, 800),
        new("identity.roles", IdentityPermissions.Module, "nav.roles", "/identity/roles", "shield-check", IdentityPermissions.Roles.Read, 810),
    ];
}
