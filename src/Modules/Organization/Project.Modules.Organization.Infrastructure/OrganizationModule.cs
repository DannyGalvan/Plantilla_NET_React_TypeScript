using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Application;
using Project.Application.Organization;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Organization.Application;
using Project.Modules.Organization.Contracts;
using Project.Modules.Organization.Infrastructure.Persistence;
using Project.Modules.Organization.Infrastructure.Services;
using Project.Web.Crud;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Organization.Infrastructure;

public sealed record CreateUnitRequest(string TypeCode, Guid? ParentId, string Code, string Name, string? CountryCode);

public sealed record UpdateUnitRequest(string Code, string Name, string? CountryCode, bool IsActive, Guid? ConcurrencyStamp);

public sealed record MoveUnitRequest(Guid? NewParentId);

public sealed record AssignUserRequest(Guid UserId);

/// <summary>
/// Optional module: companies, countries, brands and branches inside a tenant. Removing it from
/// <c>AddModules</c> disables the organization filter everywhere (no code changes in business modules).
/// </summary>
public sealed class OrganizationModule : IModule
{
    public string Name => "Organization";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<OrganizationDbContext>(Name, DataScope);
        services.AddRepositoriesFor<OrganizationDbContext>();
        services.AddRepository<OrganizationDbContext, Domain.OrganizationUnitType, Guid>();
        services.AddRepository<OrganizationDbContext, Domain.UnitAssignment, Guid>();
        services.AddModuleConventions(typeof(OrganizationPermissionProvider).Assembly);

        services.AddScoped<OrganizationTreeCache>();
        services.AddScoped<IOrganizationCache>(sp => sp.GetRequiredService<OrganizationTreeCache>());
        services.Replace(ServiceDescriptor.Scoped<IOrganizationContext, OrganizationContext>());
        services.Replace(ServiceDescriptor.Scoped<IOrganizationDirectory, OrganizationDirectory>());
        services.AddScoped<IDataSeeder, OrganizationSeeder>();
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        api.MapCrud<Guid, UnitTypeDto, CreateUnitTypeRequest, UpdateUnitTypeRequest>("organization/unit-types", OrganizationPermissions.UnitTypes.Prefix);

        var units = api.MapGroup("organization/units").WithTags("units");

        units.MapGet("/", async (string? search, ISender sender, CancellationToken ct) => (await sender.Send(new ListUnitsQuery(search), ct)).ToHttp())
            .RequirePermission(OrganizationPermissions.Units.Read)
            .WithName("ListUnits");

        units.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new GetUnitQuery(id), ct)).ToHttp())
            .RequirePermission(OrganizationPermissions.Units.Read)
            .WithName("GetUnit");

        units.MapPost("/", async (CreateUnitRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CreateUnitCommand(request.TypeCode, request.ParentId, request.Code, request.Name, request.CountryCode), ct))
                .ToCreated(u => $"/api/v1/organization/units/{u.Id}"))
            .RequirePermission(OrganizationPermissions.Units.Create)
            .WithName("CreateUnit");

        units.MapPut("/{id:guid}", async (Guid id, UpdateUnitRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateUnitCommand(id, request.Code, request.Name, request.CountryCode, request.IsActive, request.ConcurrencyStamp), ct)).ToHttp())
            .RequirePermission(OrganizationPermissions.Units.Update)
            .WithName("UpdateUnit");

        units.MapPost("/{id:guid}/move", async (Guid id, MoveUnitRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new MoveUnitCommand(id, request.NewParentId), ct)).ToHttp())
            .RequirePermission(OrganizationPermissions.Units.Update)
            .WithName("MoveUnit");

        units.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new DeleteUnitCommand(id), ct)).ToHttp())
            .RequirePermission(OrganizationPermissions.Units.Delete)
            .WithName("DeleteUnit");

        units.MapGet("/{id:guid}/users", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new ListUnitUsersQuery(id), ct)).ToHttp())
            .RequirePermission(OrganizationPermissions.Units.Read)
            .WithName("ListUnitUsers");

        units.MapPost("/{id:guid}/users", async (Guid id, AssignUserRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AssignUserToUnitCommand(id, request.UserId), ct)).ToHttp())
            .RequirePermission(OrganizationPermissions.Assignments.Manage)
            .WithName("AssignUserToUnit");

        units.MapDelete("/{id:guid}/users/{userId:guid}", async (Guid id, Guid userId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UnassignUserFromUnitCommand(id, userId), ct)).ToHttp())
            .RequirePermission(OrganizationPermissions.Assignments.Manage)
            .WithName("UnassignUserFromUnit");

        api.MapGet("me/units", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetMyUnitsQuery(), ct)).ToHttp())
            .RequireAuthenticatedUser()
            .WithTags("me")
            .WithName("GetMyUnits");
    }
}
