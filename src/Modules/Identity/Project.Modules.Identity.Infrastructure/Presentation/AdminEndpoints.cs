using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Application;
using Project.Modules.Identity.Application.Roles;
using Project.Modules.Identity.Application.Users;
using Project.Modules.Identity.Contracts;
using Project.Web.Crud;
using Project.Web.Http;
using Project.Web.Security;

namespace Project.Modules.Identity.Infrastructure.Presentation;

public sealed record CreateUserRequest(string UserName, string Email, string? DisplayName, string Password, IReadOnlyList<Guid>? RoleIds, bool? MustChangePassword);

public sealed record UpdateUserRequest(string Email, string? DisplayName, bool IsActive, IReadOnlyList<Guid>? RoleIds, Guid? ConcurrencyStamp);

public sealed record AdminResetPasswordRequest(string NewPassword);

public sealed record AddRoleAssignmentRequest(Guid RoleId, Guid? OrganizationUnitId);

public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string>? Permissions);

public sealed record UpdateRoleRequest(string Name, string? Description, Guid? ConcurrencyStamp);

public sealed record SetRolePermissionsRequest(IReadOnlyList<string> Permissions);

internal static class AdminEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        MapUsers(api.MapGroup("identity/users").WithTags("users"));
        MapRoles(api.MapGroup("identity/roles").WithTags("roles"));

        api.MapGet("identity/permissions", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListPermissionsQuery(), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Roles.Read)
            .WithTags("roles")
            .WithName("ListPermissions");
    }

    private static PageRequest ToPage(MapCrudExtensions.ListQuery query) =>
        new(query.Page ?? 1, query.PageSize ?? PaginationDefaults.DefaultPageSize, query.Sort, query.Filter, query.IncludeTotal ?? true);

    private static void MapUsers(RouteGroupBuilder users)
    {
        users.MapGet("/", async ([AsParameters] MapCrudExtensions.ListQuery query, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListUsersQuery(ToPage(query)), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.Read)
            .WithName("ListUsers");

        users.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new GetUserQuery(id), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.Read)
            .WithName("GetUser");

        users.MapPost("/", async (CreateUserRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CreateUserCommand(request.UserName, request.Email, request.DisplayName, request.Password, request.RoleIds, request.MustChangePassword ?? true), ct))
                .ToCreated(u => $"/api/v1/identity/users/{u.Id}"))
            .RequirePermission(IdentityPermissions.Users.Create)
            .WithName("CreateUser");

        users.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateUserCommand(id, request.Email, request.DisplayName, request.IsActive, request.RoleIds, request.ConcurrencyStamp), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.Update)
            .WithName("UpdateUser");

        users.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new DeleteUserCommand(id), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.Delete)
            .WithName("DeleteUser");

        users.MapPost("/{id:guid}/restore", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new RestoreUserCommand(id), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.Restore)
            .WithName("RestoreUser");

        users.MapPost("/{id:guid}/unlock", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new UnlockUserCommand(id), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.Update)
            .WithName("UnlockUser");

        users.MapPost("/{id:guid}/reset-password", async (Guid id, AdminResetPasswordRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AdminResetPasswordCommand(id, request.NewPassword), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.ResetPassword)
            .WithName("AdminResetPassword");

        users.MapPost("/{id:guid}/role-assignments", async (Guid id, AddRoleAssignmentRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddRoleAssignmentCommand(id, request.RoleId, request.OrganizationUnitId), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.Update)
            .WithName("AddRoleAssignment");

        users.MapDelete("/{id:guid}/role-assignments/{assignmentId:guid}", async (Guid id, Guid assignmentId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemoveRoleAssignmentCommand(id, assignmentId), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Users.Update)
            .WithName("RemoveRoleAssignment");
    }

    private static void MapRoles(RouteGroupBuilder roles)
    {
        roles.MapGet("/", async ([AsParameters] MapCrudExtensions.ListQuery query, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListRolesQuery(ToPage(query)), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Roles.Read)
            .WithName("ListRoles");

        roles.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new GetRoleQuery(id), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Roles.Read)
            .WithName("GetRole");

        roles.MapPost("/", async (CreateRoleRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CreateRoleCommand(request.Name, request.Description, request.Permissions), ct)).ToCreated(r => $"/api/v1/identity/roles/{r.Id}"))
            .RequirePermission(IdentityPermissions.Roles.Create)
            .WithName("CreateRole");

        roles.MapPut("/{id:guid}", async (Guid id, UpdateRoleRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateRoleCommand(id, request.Name, request.Description, request.ConcurrencyStamp), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Roles.Update)
            .WithName("UpdateRole");

        roles.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new DeleteRoleCommand(id), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Roles.Delete)
            .WithName("DeleteRole");

        roles.MapPost("/{id:guid}/restore", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new RestoreRoleCommand(id), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Roles.Restore)
            .WithName("RestoreRole");

        roles.MapPut("/{id:guid}/permissions", async (Guid id, SetRolePermissionsRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetRolePermissionsCommand(id, request.Permissions), ct)).ToHttp())
            .RequirePermission(IdentityPermissions.Roles.AssignPermissions)
            .WithName("SetRolePermissions");
    }
}
