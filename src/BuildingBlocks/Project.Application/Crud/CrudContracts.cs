using System.Text.Json.Nodes;
using Project.SharedKernel;

namespace Project.Application.Crud;

/// <summary>
/// Mapping contract of a CRUD resource, usually implemented by a Mapperly <c>[Mapper]</c> partial class.
/// </summary>
public interface ICrudMapper<TEntity, TDto, in TCreate, TUpdate>
{
    TDto ToDto(TEntity entity);

    /// <summary>Server-side projection (translated to SQL; only the DTO columns are read).</summary>
    IQueryable<TDto> ProjectToDto(IQueryable<TEntity> query);

    TEntity ToEntity(TCreate input);

    /// <summary>Copies the update DTO onto the tracked entity.</summary>
    void Apply(TUpdate input, TEntity entity);

    /// <summary>Current state as an update DTO, the base for JSON Merge Patch.</summary>
    TUpdate ToUpdate(TEntity entity);
}

/// <summary>Update DTOs that carry the concurrency stamp read by the client (409 on mismatch).</summary>
public interface IConcurrencyAware
{
    Guid? ConcurrencyStamp { get; }
}

/// <summary>All rows the caller can see, or only the rows owned by the caller (<c>/me</c> endpoints).</summary>
public enum CrudScope
{
    All,
    Owned,
}

/// <summary>Generic CRUD use cases used by <c>MapCrud</c>. One instance per resource.</summary>
public interface ICrudService<TId, TDto, in TCreate, in TUpdate>
    where TId : notnull
{
    Task<Result<PagedResult<TDto>>> ListAsync(PageRequest request, CrudScope scope, CancellationToken ct);

    Task<Result<TDto>> GetAsync(TId id, CrudScope scope, CancellationToken ct);

    Task<Result<TDto>> CreateAsync(TCreate input, CrudScope scope, CancellationToken ct);

    Task<Result<TDto>> UpdateAsync(TId id, TUpdate input, CrudScope scope, CancellationToken ct);

    /// <summary>JSON Merge Patch (RFC 7386): only the sent properties change, explicit nulls clear values.</summary>
    Task<Result<TDto>> PatchAsync(TId id, JsonObject patch, CrudScope scope, CancellationToken ct);

    Task<Result> DeleteAsync(TId id, CrudScope scope, CancellationToken ct);

    Task<Result> RestoreAsync(TId id, CrudScope scope, CancellationToken ct);
}

public sealed class CrudOptions
{
    public const string SectionName = "Pagination";

    public int MaxPageSize { get; set; } = PaginationDefaults.MaxPageSize;
}
