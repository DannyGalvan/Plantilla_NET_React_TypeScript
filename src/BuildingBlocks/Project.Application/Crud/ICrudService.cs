using Project.SharedKernel;

namespace Project.Application.Crud;

/// <summary>
/// Generic CRUD service invoked by the <c>MapCrud</c> endpoints. The handlers
/// live in this service (not in Mediator) because the source-generated
/// pipeline does not handle open generic handlers well. Validation and
/// transaction go through the same Mediator behaviors via the
/// <see cref="CrudPipeline"/>.
/// </summary>
public interface ICrudService<TEntity, TId, TDto, TCreateDto, TUpdateDto>
    where TEntity : class
{
    Task<PagedResult<TDto>> GetPagedAsync(PageRequest page, CancellationToken ct);

    Task<TDto?> GetByIdAsync(TId id, CancellationToken ct);

    Task<Result<TDto>> CreateAsync(TCreateDto input, CancellationToken ct);

    Task<Result<TDto>> UpdateAsync(TId id, TUpdateDto input, CancellationToken ct);

    Task<Result<TDto>> PatchAsync(TId id, TUpdateDto input, CancellationToken ct);

    Task<Result> DeleteAsync(TId id, CancellationToken ct);

    Task<Result> RestoreAsync(TId id, CancellationToken ct);
}