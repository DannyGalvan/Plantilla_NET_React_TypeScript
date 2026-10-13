using FluentValidation;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Organization;
using Project.Application.Security;
using Project.Modules.Identity.Contracts;
using Project.Modules.Organization.Contracts;
using Project.Modules.Organization.Domain;
using Project.SharedKernel;

namespace Project.Modules.Organization.Application;

public sealed record UnitDto(
    Guid Id,
    string TypeCode,
    Guid? ParentId,
    int Depth,
    string Code,
    string Name,
    string? CountryCode,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid ConcurrencyStamp)
{
    public static UnitDto From(OrganizationUnit unit) =>
        new(unit.Id, unit.TypeCode, unit.ParentId, unit.Depth, unit.Code, unit.Name, unit.CountryCode, unit.IsActive, unit.CreatedAt, unit.ConcurrencyStamp);
}

/// <summary>Clears cached organization data (tree, accessible units) after changes.</summary>
public interface IOrganizationCache
{
    Task InvalidateAsync(CancellationToken ct);
}

/// <summary>Permissions and navigation of the Organization module.</summary>
public sealed class OrganizationPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(OrganizationPermissions.Units.Read, OrganizationPermissions.Module, "permissions.organization.units.read", "units"),
        new(OrganizationPermissions.Units.Create, OrganizationPermissions.Module, "permissions.organization.units.create", "units"),
        new(OrganizationPermissions.Units.Update, OrganizationPermissions.Module, "permissions.organization.units.update", "units"),
        new(OrganizationPermissions.Units.Delete, OrganizationPermissions.Module, "permissions.organization.units.delete", "units"),
        new(OrganizationPermissions.Units.Restore, OrganizationPermissions.Module, "permissions.organization.units.restore", "units"),
        new(OrganizationPermissions.UnitTypes.Read, OrganizationPermissions.Module, "permissions.organization.unit-types.read", "unit-types"),
        new(OrganizationPermissions.UnitTypes.Create, OrganizationPermissions.Module, "permissions.organization.unit-types.create", "unit-types"),
        new(OrganizationPermissions.UnitTypes.Update, OrganizationPermissions.Module, "permissions.organization.unit-types.update", "unit-types"),
        new(OrganizationPermissions.UnitTypes.Delete, OrganizationPermissions.Module, "permissions.organization.unit-types.delete", "unit-types"),
        new(OrganizationPermissions.UnitTypes.Restore, OrganizationPermissions.Module, "permissions.organization.unit-types.restore", "unit-types"),
        new(OrganizationPermissions.Assignments.Manage, OrganizationPermissions.Module, "permissions.organization.assignments.manage", "units"),
        new(OrganizationPermissions.ScopeAll, OrganizationPermissions.Module, "permissions.organization.scope.all", "scope"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("organization.units", OrganizationPermissions.Module, "nav.organization", "/organization/units", "network", OrganizationPermissions.Units.Read, 700),
        new("organization.unit-types", OrganizationPermissions.Module, "nav.unitTypes", "/organization/unit-types", "layers", OrganizationPermissions.UnitTypes.Read, 710),
    ];
}

internal static class UnitAccess
{
    /// <summary>Units outside the caller's scope are reported as not found.</summary>
    public static bool CanSee(IOrganizationContext organization, Guid unitId) =>
        organization.HasFullAccess || organization.AccessibleUnitIds.Contains(unitId);
}

public sealed record ListUnitsQuery(string? Search) : IQuery<IReadOnlyList<UnitDto>>;

public sealed class ListUnitsHandler(IRepository<OrganizationUnit, Guid> units, IOrganizationContext organization)
    : IQueryHandler<ListUnitsQuery, IReadOnlyList<UnitDto>>
{
    public async ValueTask<Result<IReadOnlyList<UnitDto>>> Handle(ListUnitsQuery query, CancellationToken cancellationToken)
    {
        var spec = new Specification<OrganizationUnit>().OrderBy(u => u.Path);
        if (!organization.HasFullAccess)
        {
            var visible = organization.AccessibleUnitIds.ToList();
            spec.Where(u => visible.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            spec.Where(u => u.Name.Contains(term) || u.Code.Contains(term));
        }

        IReadOnlyList<UnitDto> items = (await units.ListAsync(spec, cancellationToken)).Select(UnitDto.From).ToList();
        return Result<IReadOnlyList<UnitDto>>.Success(items);
    }
}

public sealed record GetUnitQuery(Guid Id) : IQuery<UnitDto>;

public sealed class GetUnitHandler(IRepository<OrganizationUnit, Guid> units, IOrganizationContext organization) : IQueryHandler<GetUnitQuery, UnitDto>
{
    public async ValueTask<Result<UnitDto>> Handle(GetUnitQuery query, CancellationToken cancellationToken)
    {
        var unit = await units.FirstOrDefaultAsync(new Specification<OrganizationUnit>().Where(u => u.Id == query.Id), cancellationToken);
        return unit is null || !UnitAccess.CanSee(organization, unit.Id) ? Error.NotFound("organization.unit_not_found") : UnitDto.From(unit);
    }
}

public sealed record CreateUnitCommand(string TypeCode, Guid? ParentId, string Code, string Name, string? CountryCode) : ICommand<UnitDto>;

public sealed class CreateUnitValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitValidator()
    {
        RuleFor(c => c.TypeCode).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.CountryCode).Length(2).When(c => !string.IsNullOrEmpty(c.CountryCode));
    }
}

public sealed class CreateUnitHandler(
    IRepository<OrganizationUnit, Guid> units,
    IRepository<OrganizationUnitType, Guid> types,
    IOrganizationContext organization,
    IOrganizationCache cache) : ICommandHandler<CreateUnitCommand, UnitDto>
{
    public async ValueTask<Result<UnitDto>> Handle(CreateUnitCommand command, CancellationToken cancellationToken)
    {
        var typeCode = command.TypeCode.Trim().ToLowerInvariant();
        var type = await types.FirstOrDefaultAsync(new Specification<OrganizationUnitType>().Where(t => t.Code == typeCode), cancellationToken);
        if (type is null)
        {
            return Error.Validation("typeCode", "The unit type does not exist.");
        }

        OrganizationUnit? parent = null;
        if (command.ParentId is { } parentId)
        {
            parent = await units.FirstOrDefaultAsync(new Specification<OrganizationUnit>().Where(u => u.Id == parentId), cancellationToken);
            if (parent is null || !UnitAccess.CanSee(organization, parentId))
            {
                return Error.Validation("parentId", "The parent unit does not exist.");
            }
        }
        else if (!organization.HasFullAccess)
        {
            return Error.Forbidden("organization.root_forbidden", "Only users with access to the whole organization can create root units.");
        }

        var code = command.Code.Trim();
        if (await units.AnyAsync(new Specification<OrganizationUnit>().Where(u => u.Code == code).WithDeleted(), cancellationToken))
        {
            return Error.Conflict("organization.unit_code_taken", "Another unit already uses this code.");
        }

        var created = OrganizationUnit.Create(type, parent, command.Code, command.Name, command.CountryCode);
        if (created.IsFailure)
        {
            return created.Error!;
        }

        units.Add(created.Value);
        await units.UnitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return UnitDto.From(created.Value);
    }
}

public sealed record UpdateUnitCommand(Guid Id, string Code, string Name, string? CountryCode, bool IsActive, Guid? ConcurrencyStamp) : ICommand<UnitDto>;

public sealed class UpdateUnitValidator : AbstractValidator<UpdateUnitCommand>
{
    public UpdateUnitValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.CountryCode).Length(2).When(c => !string.IsNullOrEmpty(c.CountryCode));
    }
}

public sealed class UpdateUnitHandler(IRepository<OrganizationUnit, Guid> units, IOrganizationContext organization, IOrganizationCache cache)
    : ICommandHandler<UpdateUnitCommand, UnitDto>
{
    public async ValueTask<Result<UnitDto>> Handle(UpdateUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await units.FirstOrDefaultAsync(new Specification<OrganizationUnit>().Where(u => u.Id == command.Id).Tracked(), cancellationToken);
        if (unit is null || !UnitAccess.CanSee(organization, unit.Id))
        {
            return Error.NotFound("organization.unit_not_found");
        }

        if (command.ConcurrencyStamp is { } stamp && stamp != unit.ConcurrencyStamp)
        {
            return Error.Conflict("concurrency.conflict", "The unit was modified by someone else. Reload it and try again.");
        }

        var code = command.Code.Trim();
        if (await units.AnyAsync(new Specification<OrganizationUnit>().Where(u => u.Code == code && u.Id != unit.Id).WithDeleted(), cancellationToken))
        {
            return Error.Conflict("organization.unit_code_taken", "Another unit already uses this code.");
        }

        unit.Update(command.Code, command.Name, command.CountryCode, command.IsActive);
        await units.UnitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return UnitDto.From(unit);
    }
}

/// <summary>Moves a unit and rewrites the materialized path of every descendant in one transaction.</summary>
public sealed record MoveUnitCommand(Guid Id, Guid? NewParentId) : ICommand<UnitDto>;

public sealed class MoveUnitHandler(
    IRepository<OrganizationUnit, Guid> units,
    IRepository<OrganizationUnitType, Guid> types,
    IOrganizationContext organization,
    IOrganizationCache cache) : ICommandHandler<MoveUnitCommand, UnitDto>
{
    public async ValueTask<Result<UnitDto>> Handle(MoveUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await units.FirstOrDefaultAsync(new Specification<OrganizationUnit>().Where(u => u.Id == command.Id).Tracked(), cancellationToken);
        if (unit is null || !UnitAccess.CanSee(organization, unit.Id))
        {
            return Error.NotFound("organization.unit_not_found");
        }

        OrganizationUnit? parent = null;
        if (command.NewParentId is { } parentId)
        {
            parent = await units.FirstOrDefaultAsync(new Specification<OrganizationUnit>().Where(u => u.Id == parentId), cancellationToken);
            if (parent is null || !UnitAccess.CanSee(organization, parentId))
            {
                return Error.Validation("newParentId", "The parent unit does not exist.");
            }
        }

        var typeCode = unit.TypeCode;
        var type = await types.FirstOrDefaultAsync(new Specification<OrganizationUnitType>().Where(t => t.Code == typeCode), cancellationToken);
        if (type is null)
        {
            return Error.Validation("typeCode", "The unit type no longer exists.");
        }

        var oldPath = unit.Path;
        var oldDepth = unit.Depth;
        var moved = unit.MoveTo(type, parent);
        if (moved.IsFailure)
        {
            return moved.Error!;
        }

        var descendants = await units.ListAsync(
            new Specification<OrganizationUnit>().Where(u => u.Path.StartsWith(oldPath) && u.Id != unit.Id).Tracked(),
            cancellationToken);
        foreach (var descendant in descendants)
        {
            descendant.RebasePath(oldPath, unit.Path, unit.Depth - oldDepth);
        }

        await units.UnitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return UnitDto.From(unit);
    }
}

public sealed record DeleteUnitCommand(Guid Id) : ICommand;

public sealed class DeleteUnitHandler(IRepository<OrganizationUnit, Guid> units, IOrganizationContext organization, IOrganizationCache cache)
    : ICommandHandler<DeleteUnitCommand>
{
    public async ValueTask<Result> Handle(DeleteUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await units.FirstOrDefaultAsync(new Specification<OrganizationUnit>().Where(u => u.Id == command.Id).Tracked(), cancellationToken);
        if (unit is null || !UnitAccess.CanSee(organization, unit.Id))
        {
            return Error.NotFound("organization.unit_not_found");
        }

        if (await units.AnyAsync(new Specification<OrganizationUnit>().Where(u => u.ParentId == unit.Id), cancellationToken))
        {
            return Error.Conflict("organization.unit_has_children", "Move or delete the child units first.");
        }

        units.Remove(unit);
        await units.UnitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record ListUnitUsersQuery(Guid UnitId) : IQuery<IReadOnlyList<UserSummary>>;

public sealed class ListUnitUsersHandler(
    IRepository<UnitAssignment, Guid> assignments,
    IOrganizationContext organization,
    IUserDirectory users) : IQueryHandler<ListUnitUsersQuery, IReadOnlyList<UserSummary>>
{
    public async ValueTask<Result<IReadOnlyList<UserSummary>>> Handle(ListUnitUsersQuery query, CancellationToken cancellationToken)
    {
        if (!UnitAccess.CanSee(organization, query.UnitId))
        {
            return Error.NotFound("organization.unit_not_found");
        }

        var userIds = (await assignments.ListAsync(new Specification<UnitAssignment>().Where(a => a.UnitId == query.UnitId), cancellationToken))
            .Select(a => a.UserId)
            .ToList();
        return Result<IReadOnlyList<UserSummary>>.Success(await users.FindAsync(userIds, cancellationToken));
    }
}

public sealed record AssignUserToUnitCommand(Guid UnitId, Guid UserId) : ICommand;

public sealed class AssignUserToUnitHandler(
    IRepository<UnitAssignment, Guid> assignments,
    IRepository<OrganizationUnit, Guid> units,
    IOrganizationContext organization,
    IUserDirectory users,
    IOrganizationCache cache) : ICommandHandler<AssignUserToUnitCommand>
{
    public async ValueTask<Result> Handle(AssignUserToUnitCommand command, CancellationToken cancellationToken)
    {
        if (!UnitAccess.CanSee(organization, command.UnitId)
            || !await units.AnyAsync(new Specification<OrganizationUnit>().Where(u => u.Id == command.UnitId), cancellationToken))
        {
            return Error.NotFound("organization.unit_not_found");
        }

        if (!await users.IsMemberAsync(command.UserId, cancellationToken))
        {
            return Error.Validation("userId", "The user does not belong to this tenant.");
        }

        if (!await assignments.AnyAsync(new Specification<UnitAssignment>().Where(a => a.UnitId == command.UnitId && a.UserId == command.UserId), cancellationToken))
        {
            assignments.Add(UnitAssignment.Create(command.UserId, command.UnitId));
            await assignments.UnitOfWork.SaveChangesAsync(cancellationToken);
            await cache.InvalidateAsync(cancellationToken);
        }

        return Result.Success();
    }
}

public sealed record UnassignUserFromUnitCommand(Guid UnitId, Guid UserId) : ICommand;

public sealed class UnassignUserFromUnitHandler(
    IRepository<UnitAssignment, Guid> assignments,
    IOrganizationContext organization,
    IOrganizationCache cache) : ICommandHandler<UnassignUserFromUnitCommand>
{
    public async ValueTask<Result> Handle(UnassignUserFromUnitCommand command, CancellationToken cancellationToken)
    {
        if (!UnitAccess.CanSee(organization, command.UnitId))
        {
            return Error.NotFound("organization.unit_not_found");
        }

        var assignment = await assignments.FirstOrDefaultAsync(
            new Specification<UnitAssignment>().Where(a => a.UnitId == command.UnitId && a.UserId == command.UserId).Tracked(),
            cancellationToken);
        if (assignment is null)
        {
            return Error.NotFound("organization.assignment_not_found");
        }

        assignments.Remove(assignment);
        await assignments.UnitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// Units the signed-in user can select in the context switcher: every unit with <c>organization.scope.all</c>,
/// otherwise the assigned units and their descendants (independent of the unit currently selected).
/// </summary>
public sealed record GetMyUnitsQuery : IQuery<IReadOnlyList<UnitDto>>;

public sealed class GetMyUnitsHandler(
    IRepository<OrganizationUnit, Guid> units,
    IOrganizationDirectory directory,
    IPermissionService permissions,
    ICurrentUser currentUser) : IQueryHandler<GetMyUnitsQuery, IReadOnlyList<UnitDto>>
{
    public async ValueTask<Result<IReadOnlyList<UnitDto>>> Handle(GetMyUnitsQuery query, CancellationToken cancellationToken)
    {
        var spec = new Specification<OrganizationUnit>().Where(u => u.IsActive).OrderBy(u => u.Path);
        if (!await permissions.HasPermissionAsync(OrganizationPermissions.ScopeAll, cancellationToken))
        {
            var assigned = await directory.GetAssignedUnitsAsync(currentUser.UserId ?? string.Empty, cancellationToken);
            var selectable = (await directory.ExpandDescendantsAsync(assigned.ToList(), cancellationToken)).ToList();
            spec.Where(u => selectable.Contains(u.Id));
        }

        IReadOnlyList<UnitDto> items = (await units.ListAsync(spec, cancellationToken)).Select(UnitDto.From).ToList();
        return Result<IReadOnlyList<UnitDto>>.Success(items);
    }
}
