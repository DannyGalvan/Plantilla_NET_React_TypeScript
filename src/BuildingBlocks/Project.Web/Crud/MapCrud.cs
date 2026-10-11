using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Crud;
using Project.SharedKernel;

namespace Project.Web.Crud;

/// <summary>
/// Wires the standard CRUD endpoints on an <c>IEndpointRouteBuilder</c> for a
/// given entity. Automatically applies versioning, OpenAPI metadata and
/// <c>RequirePermission(...)</c> per operation via <see cref="PermissionAttribute"/>.
/// </summary>
public static class MapCrudExtensions
{
    public static IEndpointRouteBuilder MapCrud<TEntity, TId, TDto, TCreateDto, TUpdateDto>(
        this IEndpointRouteBuilder endpoints,
        string resource,
        string permissionPrefix)
        where TEntity : Project.SharedKernel.Domain.AggregateRoot<TId>
        where TId : notnull
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        var group = endpoints.MapGroup($"/api/v1/{resource}");

        group.MapGet("/", async (
                ICrudService<TEntity, TId, TDto, TCreateDto, TUpdateDto> svc,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] string? sort,
                [FromQuery] string? filter,
                CancellationToken ct) =>
            {
                var pageReq = new PageRequest(
                    page ?? 1,
                    pageSize ?? PaginationDefaults.DefaultPageSize,
                    sort,
                    filter);
                return await svc.GetPagedAsync(pageReq, ct);
            })
            .WithName($"List{resource}")
            .WithMetadata(new PermissionAttribute($"{permissionPrefix}.read"));

        group.MapGet("/{id}", async (
                ICrudService<TEntity, TId, TDto, TCreateDto, TUpdateDto> svc,
                TId id,
                CancellationToken ct) =>
            {
                var result = await svc.GetByIdAsync(id, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithName($"Get{resource}")
            .WithMetadata(new PermissionAttribute($"{permissionPrefix}.read"));

        group.MapPost("/", async (
                ICrudService<TEntity, TId, TDto, TCreateDto, TUpdateDto> svc,
                [FromBody] TCreateDto input,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var result = await svc.CreateAsync(input, ct);
                return result.ToHttp(httpContext);
            })
            .WithName($"Create{resource}")
            .WithMetadata(new PermissionAttribute($"{permissionPrefix}.create"));

        group.MapPut("/{id}", async (
                ICrudService<TEntity, TId, TDto, TCreateDto, TUpdateDto> svc,
                TId id,
                [FromBody] TUpdateDto input,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var result = await svc.UpdateAsync(id, input, ct);
                return result.ToHttp(httpContext);
            })
            .WithName($"Update{resource}")
            .WithMetadata(new PermissionAttribute($"{permissionPrefix}.update"));

        group.MapDelete("/{id}", async (
                ICrudService<TEntity, TId, TDto, TCreateDto, TUpdateDto> svc,
                TId id,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var result = await svc.DeleteAsync(id, ct);
                return result.ToHttp(httpContext);
            })
            .WithName($"Delete{resource}")
            .WithMetadata(new PermissionAttribute($"{permissionPrefix}.delete"));

        return endpoints;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PermissionAttribute : Attribute
{
    public string Key { get; }

    public PermissionAttribute(string key)
    {
        Key = key;
    }
}