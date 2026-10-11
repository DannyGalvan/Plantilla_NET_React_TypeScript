using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Project.Application;
using Project.Application.Crud;
using Project.Web.Http;
using Project.Web.Security;

namespace Project.Web.Crud;

[Flags]
public enum CrudOperations
{
    None = 0,
    List = 1,
    Get = 2,
    Create = 4,
    Update = 8,
    Patch = 16,
    Delete = 32,
    Restore = 64,
    Read = List | Get,
    All = List | Get | Create | Update | Patch | Delete | Restore,
}

/// <summary>Configures what <see cref="MapCrudExtensions.MapCrud{TId,TDto,TCreate,TUpdate}"/> exposes.</summary>
public sealed class CrudEndpointOptions
{
    public CrudOperations Operations { get; set; } = CrudOperations.All;

    /// <summary>Also maps <c>/me/{resource}</c> endpoints restricted to rows owned by the caller (entity implements IOwned).</summary>
    public bool OwnedEndpoints { get; set; }

    /// <summary>Hidden (404) when this feature is disabled for the tenant.</summary>
    public string? Feature { get; set; }
}

public static class MapCrudExtensions
{
    /// <summary>
    /// Maps the standard REST endpoints of a resource and protects each one with
    /// <c>{permissionPrefix}.read|create|update|delete|restore</c>:
    /// <code>
    /// api.MapCrud&lt;Guid, ProductDto, CreateProductRequest, UpdateProductRequest&gt;("catalog/products", CatalogPermissions.Products.Prefix);
    /// </code>
    /// </summary>
    public static RouteGroupBuilder MapCrud<TId, TDto, TCreate, TUpdate>(
        this IEndpointRouteBuilder api,
        string resource,
        string permissionPrefix,
        Action<CrudEndpointOptions>? configure = null)
        where TId : notnull
        where TCreate : notnull
        where TUpdate : notnull
    {
        var options = new CrudEndpointOptions();
        configure?.Invoke(options);

        var tag = resource.Split('/')[^1];
        var group = api.MapGroup(resource).WithTags(tag);
        if (options.Feature is not null)
        {
            group.RequireFeature(options.Feature);
        }

        MapOperations<TId, TDto, TCreate, TUpdate>(group, resource, tag, permissionPrefix, options.Operations, CrudScope.All);

        if (options.OwnedEndpoints)
        {
            var owned = api.MapGroup($"me/{resource}").WithTags($"me-{tag}");
            MapOperations<TId, TDto, TCreate, TUpdate>(owned, $"me/{resource}", $"Own{tag}", null, options.Operations & ~CrudOperations.Restore, CrudScope.Owned);
        }

        return group;
    }

    private static void MapOperations<TId, TDto, TCreate, TUpdate>(
        RouteGroupBuilder group,
        string resource,
        string name,
        string? permissionPrefix,
        CrudOperations operations,
        CrudScope scope)
        where TId : notnull
        where TCreate : notnull
        where TUpdate : notnull
    {
        RouteHandlerBuilder Secure(RouteHandlerBuilder builder, string action) =>
            permissionPrefix is null ? builder.RequireAuthenticatedUser() : builder.RequirePermission($"{permissionPrefix}.{action}");

        var operationName = Pascal(name);

        if (operations.HasFlag(CrudOperations.List))
        {
            Secure(group.MapGet("/", async (
                    [AsParameters] ListQuery query,
                    ICrudService<TId, TDto, TCreate, TUpdate> service,
                    CancellationToken ct) =>
                {
                    var request = new PageRequest(query.Page ?? 1, query.PageSize ?? PaginationDefaults.DefaultPageSize, query.Sort, query.Filter, query.IncludeTotal ?? true);
                    return (await service.ListAsync(request, scope, ct)).ToHttp();
                })
                .WithName($"List{operationName}")
                .Produces<PagedResult<TDto>>(), "read");
        }

        if (operations.HasFlag(CrudOperations.Get))
        {
            Secure(group.MapGet("/{id}", async (TId id, ICrudService<TId, TDto, TCreate, TUpdate> service, CancellationToken ct) =>
                    (await service.GetAsync(id, scope, ct)).ToHttp())
                .WithName($"Get{operationName}")
                .Produces<TDto>(), "read");
        }

        if (operations.HasFlag(CrudOperations.Create))
        {
            Secure(group.MapPost("/", async (TCreate input, ICrudService<TId, TDto, TCreate, TUpdate> service, HttpContext http, CancellationToken ct) =>
                    (await service.CreateAsync(input, scope, ct)).ToCreated(dto => $"{http.Request.Path.ToString().TrimEnd('/')}/{IdOf(dto)}"))
                .WithName($"Create{operationName}")
                .Produces<TDto>(StatusCodes.Status201Created), "create");
        }

        if (operations.HasFlag(CrudOperations.Update))
        {
            Secure(group.MapPut("/{id}", async (TId id, TUpdate input, ICrudService<TId, TDto, TCreate, TUpdate> service, CancellationToken ct) =>
                    (await service.UpdateAsync(id, input, scope, ct)).ToHttp())
                .WithName($"Update{operationName}")
                .Produces<TDto>(), "update");
        }

        if (operations.HasFlag(CrudOperations.Patch))
        {
            Secure(group.MapPatch("/{id}", async (TId id, [FromBody] JsonObject patch, ICrudService<TId, TDto, TCreate, TUpdate> service, CancellationToken ct) =>
                    (await service.PatchAsync(id, patch, scope, ct)).ToHttp())
                .WithName($"Patch{operationName}")
                .Accepts<JsonObject>("application/merge-patch+json", "application/json")
                .Produces<TDto>(), "update");
        }

        if (operations.HasFlag(CrudOperations.Delete))
        {
            Secure(group.MapDelete("/{id}", async (TId id, ICrudService<TId, TDto, TCreate, TUpdate> service, CancellationToken ct) =>
                    (await service.DeleteAsync(id, scope, ct)).ToHttp())
                .WithName($"Delete{operationName}"), "delete");
        }

        if (operations.HasFlag(CrudOperations.Restore))
        {
            Secure(group.MapPost("/{id}/restore", async (TId id, ICrudService<TId, TDto, TCreate, TUpdate> service, CancellationToken ct) =>
                    (await service.RestoreAsync(id, scope, ct)).ToHttp())
                .WithName($"Restore{operationName}"), "restore");
        }
    }

    private static object? IdOf<TDto>(TDto dto) => typeof(TDto).GetProperty("Id")?.GetValue(dto);

    private static string Pascal(string value) =>
        string.Concat(value.Split(['-', '_', '/'], StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));

    /// <summary>Query string of list endpoints.</summary>
    public sealed record ListQuery(int? Page, int? PageSize, string? Sort, string? Filter, bool? IncludeTotal);
}
