using FluentValidation;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Tenancy;
using Project.Modules.Identity.Application.Auth;
using Project.Modules.Identity.Domain;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application.Users;

/// <summary>Tenant scoping shared by the user use cases: a user exists for the caller only if it is a member of the tenant.</summary>
public sealed class UserScope(
    IRepository<User, Guid> users,
    IRepository<TenantMembership, Guid> memberships,
    IRepository<Role, Guid> roles,
    ITenantContext tenant)
{
    public Guid TenantId => tenant.TenantId ?? TenantDefaults.DefaultTenantId;

    public async Task<User?> FindMemberAsync(Guid userId, bool includeDeleted, CancellationToken ct)
    {
        var tenantId = TenantId;
        if (!await memberships.AnyAsync(new Specification<TenantMembership>().Where(m => m.UserId == userId && m.TenantId == tenantId), ct))
        {
            return null;
        }

        var spec = new Specification<User>().Where(u => u.Id == userId).Tracked();
        return await users.FirstOrDefaultAsync(includeDeleted ? spec.WithDeleted() : spec, ct);
    }

    /// <summary>Roles that can be assigned in the tenant: its own roles and shared (non-system) platform roles.</summary>
    public async Task<Result<IReadOnlyList<Role>>> ResolveAssignableRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        if (roleIds.Count == 0)
        {
            return Result<IReadOnlyList<Role>>.Success([]);
        }

        var tenantId = TenantId;
        var ids = roleIds.Distinct().ToList();
        var found = await roles.ListAsync(
            new Specification<Role>().Where(r => ids.Contains(r.Id) && (r.TenantId == tenantId || (r.TenantId == null && !r.IsSystem))),
            ct);

        return found.Count == ids.Count
            ? Result<IReadOnlyList<Role>>.Success(found)
            : Error.Validation("roleIds", "One or more roles do not exist in this tenant.");
    }
}

public sealed record ListUsersQuery(PageRequest Page) : IQuery<PagedResult<UserDto>>;

public sealed class ListUsersHandler(IIdentityReadService reads, UserScope scope) : IQueryHandler<ListUsersQuery, PagedResult<UserDto>>
{
    public async ValueTask<Result<PagedResult<UserDto>>> Handle(ListUsersQuery query, CancellationToken cancellationToken) =>
        await reads.ListUsersAsync(query.Page, scope.TenantId, cancellationToken);
}

public sealed record GetUserQuery(Guid Id) : IQuery<UserDto>;

public sealed class GetUserHandler(IIdentityReadService reads, UserScope scope) : IQueryHandler<GetUserQuery, UserDto>
{
    public async ValueTask<Result<UserDto>> Handle(GetUserQuery query, CancellationToken cancellationToken) =>
        await reads.GetUserAsync(query.Id, scope.TenantId, cancellationToken) is { } user ? user : Error.NotFound("user.not_found");
}

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string? DisplayName,
    string Password,
    IReadOnlyList<Guid>? RoleIds,
    bool MustChangePassword = true) : ICommand<UserDto>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(c => c.UserName).NotEmpty().MinimumLength(3).MaximumLength(64).Matches("^[a-zA-Z0-9._@-]+$")
            .WithMessage("Use letters, digits and . _ @ - only.");
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.DisplayName).MaximumLength(128);
        RuleFor(c => c.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class CreateUserHandler(
    IRepository<User, Guid> users,
    IRepository<TenantMembership, Guid> memberships,
    IRepository<RoleAssignment, Guid> assignments,
    UserScope scope,
    PasswordPolicy policy,
    IPasswordHasher hasher,
    IIdentityReadService reads,
    TimeProvider clock) : ICommandHandler<CreateUserCommand, UserDto>
{
    public async ValueTask<Result<UserDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var userName = User.Normalize(command.UserName);
        var email = User.Normalize(command.Email);
        if (await users.AnyAsync(new Specification<User>().Where(u => u.NormalizedUserName == userName).WithDeleted(), cancellationToken))
        {
            return Error.Conflict("user.username_taken", "The user name is already in use.");
        }

        if (await users.AnyAsync(new Specification<User>().Where(u => u.NormalizedEmail == email).WithDeleted(), cancellationToken))
        {
            return Error.Conflict("user.email_taken", "The email is already in use.");
        }

        var weak = await policy.ValidateAsync(command.Password, command.UserName, cancellationToken);
        if (weak is not null)
        {
            return weak;
        }

        var roles = await scope.ResolveAssignableRolesAsync(command.RoleIds ?? [], cancellationToken);
        if (roles.IsFailure)
        {
            return roles.Error!;
        }

        var now = clock.GetUtcNow();
        var user = User.Create(command.UserName, command.Email, command.DisplayName, hasher.Hash(command.Password), command.MustChangePassword, now);
        users.Add(user);
        memberships.Add(TenantMembership.Create(user.Id, scope.TenantId, isDefault: true, now));
        foreach (var role in roles.Value)
        {
            assignments.Add(RoleAssignment.Create(user.Id, role.Id, scope.TenantId, null));
        }

        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        return (await reads.GetUserAsync(user.Id, scope.TenantId, cancellationToken))!;
    }
}

public sealed record UpdateUserCommand(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsActive,
    IReadOnlyList<Guid>? RoleIds,
    Guid? ConcurrencyStamp) : ICommand<UserDto>;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.DisplayName).MaximumLength(128);
    }
}

public sealed class UpdateUserHandler(
    IRepository<User, Guid> users,
    IRepository<RoleAssignment, Guid> assignments,
    UserScope scope,
    IIdentityReadService reads,
    IPermissionCache permissionCache,
    ICurrentUser currentUser) : ICommandHandler<UpdateUserCommand, UserDto>
{
    public async ValueTask<Result<UserDto>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await scope.FindMemberAsync(command.Id, includeDeleted: false, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("user.not_found");
        }

        if (command.ConcurrencyStamp is { } stamp && stamp != user.ConcurrencyStamp)
        {
            return Error.Conflict("concurrency.conflict", "The user was modified by someone else. Reload it and try again.");
        }

        if (!command.IsActive && user.Id.ToString() == currentUser.UserId)
        {
            return Error.Validation("isActive", "You cannot deactivate your own account.");
        }

        var email = User.Normalize(command.Email);
        if (await users.AnyAsync(new Specification<User>().Where(u => u.NormalizedEmail == email && u.Id != user.Id).WithDeleted(), cancellationToken))
        {
            return Error.Conflict("user.email_taken", "The email is already in use.");
        }

        user.UpdateProfile(command.Email, command.DisplayName);
        user.SetActive(command.IsActive);

        if (command.RoleIds is not null)
        {
            var roles = await scope.ResolveAssignableRolesAsync(command.RoleIds, cancellationToken);
            if (roles.IsFailure)
            {
                return roles.Error!;
            }

            var tenantId = scope.TenantId;
            var current = await assignments.ListAsync(
                new Specification<RoleAssignment>().Where(a => a.UserId == user.Id && a.TenantId == tenantId && a.OrganizationUnitId == null).Tracked(),
                cancellationToken);
            var target = roles.Value.Select(r => r.Id).ToHashSet();

            foreach (var stale in current.Where(a => !target.Contains(a.RoleId)))
            {
                assignments.Remove(stale);
            }

            foreach (var roleId in target.Where(id => current.All(a => a.RoleId != id)))
            {
                assignments.Add(RoleAssignment.Create(user.Id, roleId, tenantId, null));
            }

            user.BumpPermissionsVersion();
        }

        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        await permissionCache.InvalidateUsersAsync([user.Id], cancellationToken);
        return (await reads.GetUserAsync(user.Id, scope.TenantId, cancellationToken))!;
    }
}

public sealed record DeleteUserCommand(Guid Id) : ICommand;

public sealed class DeleteUserHandler(
    IRepository<User, Guid> users,
    IRepository<RefreshToken, Guid> refreshTokens,
    UserScope scope,
    ICurrentUser currentUser,
    IPermissionCache permissionCache,
    TimeProvider clock) : ICommandHandler<DeleteUserCommand>
{
    public async ValueTask<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (command.Id.ToString() == currentUser.UserId)
        {
            return Error.Validation("id", "You cannot delete your own account.");
        }

        var user = await scope.FindMemberAsync(command.Id, includeDeleted: false, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("user.not_found");
        }

        users.Remove(user);
        var now = clock.GetUtcNow();
        foreach (var token in await refreshTokens.ListAsync(new Specification<RefreshToken>().Where(t => t.UserId == user.Id && t.RevokedAt == null).Tracked(), cancellationToken))
        {
            token.Revoke(now);
        }

        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        await permissionCache.InvalidateUsersAsync([user.Id], cancellationToken);
        return Result.Success();
    }
}

public sealed record RestoreUserCommand(Guid Id) : ICommand;

public sealed class RestoreUserHandler(IRepository<User, Guid> users, UserScope scope) : ICommandHandler<RestoreUserCommand>
{
    public async ValueTask<Result> Handle(RestoreUserCommand command, CancellationToken cancellationToken)
    {
        var user = await scope.FindMemberAsync(command.Id, includeDeleted: true, cancellationToken);
        if (user is not { IsDeleted: true })
        {
            return Error.NotFound("user.not_found");
        }

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.DeletedBy = null;
        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record UnlockUserCommand(Guid Id) : ICommand;

public sealed class UnlockUserHandler(IRepository<User, Guid> users, UserScope scope) : ICommandHandler<UnlockUserCommand>
{
    public async ValueTask<Result> Handle(UnlockUserCommand command, CancellationToken cancellationToken)
    {
        var user = await scope.FindMemberAsync(command.Id, includeDeleted: false, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("user.not_found");
        }

        user.Unlock();
        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>An administrator sets a temporary password; the user must change it at the next sign-in.</summary>
public sealed record AdminResetPasswordCommand(Guid Id, string NewPassword) : ICommand;

public sealed class AdminResetPasswordHandler(IRepository<User, Guid> users, UserScope scope, PasswordChanger changer)
    : ICommandHandler<AdminResetPasswordCommand>
{
    public async ValueTask<Result> Handle(AdminResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await scope.FindMemberAsync(command.Id, includeDeleted: false, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("user.not_found");
        }

        var failed = await changer.ChangeAsync(user, command.NewPassword, mustChangePassword: true, cancellationToken);
        if (failed is not null)
        {
            return failed;
        }

        user.Unlock();
        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Grants a role restricted to an organization unit (and its descendants).</summary>
public sealed record AddRoleAssignmentCommand(Guid UserId, Guid RoleId, Guid? OrganizationUnitId) : ICommand<UserDto>;

public sealed class AddRoleAssignmentHandler(
    IRepository<RoleAssignment, Guid> assignments,
    UserScope scope,
    IIdentityReadService reads,
    Project.Application.Organization.IOrganizationDirectory directory,
    IPermissionCache permissionCache) : ICommandHandler<AddRoleAssignmentCommand, UserDto>
{
    public async ValueTask<Result<UserDto>> Handle(AddRoleAssignmentCommand command, CancellationToken cancellationToken)
    {
        var user = await scope.FindMemberAsync(command.UserId, includeDeleted: false, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("user.not_found");
        }

        var roles = await scope.ResolveAssignableRolesAsync([command.RoleId], cancellationToken);
        if (roles.IsFailure)
        {
            return roles.Error!;
        }

        if (command.OrganizationUnitId is { } unit && (await directory.GetPathAsync(unit, cancellationToken)).Count == 0)
        {
            return Error.Validation("organizationUnitId", "The organization unit does not exist.");
        }

        var tenantId = scope.TenantId;
        var exists = await assignments.AnyAsync(
            new Specification<RoleAssignment>().Where(a => a.UserId == user.Id && a.RoleId == command.RoleId && a.TenantId == tenantId && a.OrganizationUnitId == command.OrganizationUnitId),
            cancellationToken);
        if (!exists)
        {
            assignments.Add(RoleAssignment.Create(user.Id, command.RoleId, tenantId, command.OrganizationUnitId));
            user.BumpPermissionsVersion();
            await assignments.UnitOfWork.SaveChangesAsync(cancellationToken);
            await permissionCache.InvalidateUsersAsync([user.Id], cancellationToken);
        }

        return (await reads.GetUserAsync(user.Id, tenantId, cancellationToken))!;
    }
}

public sealed record RemoveRoleAssignmentCommand(Guid UserId, Guid AssignmentId) : ICommand<UserDto>;

public sealed class RemoveRoleAssignmentHandler(
    IRepository<RoleAssignment, Guid> assignments,
    UserScope scope,
    IIdentityReadService reads,
    IPermissionCache permissionCache) : ICommandHandler<RemoveRoleAssignmentCommand, UserDto>
{
    public async ValueTask<Result<UserDto>> Handle(RemoveRoleAssignmentCommand command, CancellationToken cancellationToken)
    {
        var user = await scope.FindMemberAsync(command.UserId, includeDeleted: false, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("user.not_found");
        }

        var tenantId = scope.TenantId;
        var assignment = await assignments.FirstOrDefaultAsync(
            new Specification<RoleAssignment>().Where(a => a.Id == command.AssignmentId && a.UserId == user.Id && a.TenantId == tenantId).Tracked(),
            cancellationToken);
        if (assignment is null)
        {
            return Error.NotFound("assignment.not_found");
        }

        assignments.Remove(assignment);
        user.BumpPermissionsVersion();
        await assignments.UnitOfWork.SaveChangesAsync(cancellationToken);
        await permissionCache.InvalidateUsersAsync([user.Id], cancellationToken);
        return (await reads.GetUserAsync(user.Id, tenantId, cancellationToken))!;
    }
}
