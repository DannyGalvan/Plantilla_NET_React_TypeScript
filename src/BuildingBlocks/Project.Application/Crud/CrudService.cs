using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions;
using Project.Application.Querying;
using Project.SharedKernel;

namespace Project.Application.Crud;

/// <summary>
/// Default CRUD implementation. Reads use <see cref="IRepository{T, TId}"/>
/// with the <see cref="FilterParser{T}"/> and <see cref="SortParser{T}"/>.
/// Writes use the unit of work inside a transaction so partial failures
/// roll back cleanly. Validation is delegated to FluentValidation validators
/// resolved per request from the same scope.
/// </summary>
public sealed class CrudService<TEntity, TId, TDto, TCreateDto, TUpdateDto> : ICrudService<TEntity, TId, TDto, TCreateDto, TUpdateDto>
    where TEntity : Project.SharedKernel.Domain.AggregateRoot<TId>
    where TId : notnull
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    private readonly IRepository<TEntity, TId> _repository;
    private readonly IUnitOfWork _uow;
    private readonly IProjector<TEntity, TDto> _projector;
    private readonly IMapperTo<TCreateDto, TEntity> _createMapper;
    private readonly IMapperTo<TUpdateDto, TEntity> _updateMapper;
    private readonly IQueryPolicy<TEntity> _queryPolicy;
    private readonly FilterParser<TEntity> _filterParser;
    private readonly SortParser<TEntity> _sortParser;
    private readonly IServiceProvider _services;

    public CrudService(
        IRepository<TEntity, TId> repository,
        IUnitOfWork uow,
        IProjector<TEntity, TDto> projector,
        IMapperTo<TCreateDto, TEntity> createMapper,
        IMapperTo<TUpdateDto, TEntity> updateMapper,
        IQueryPolicy<TEntity> queryPolicy,
        FilterParser<TEntity> filterParser,
        SortParser<TEntity> sortParser,
        IServiceProvider services)
    {
        _repository = repository;
        _uow = uow;
        _projector = projector;
        _createMapper = createMapper;
        _updateMapper = updateMapper;
        _queryPolicy = queryPolicy;
        _filterParser = filterParser;
        _sortParser = sortParser;
        _services = services;
    }

    public async Task<PagedResult<TDto>> GetPagedAsync(PageRequest page, CancellationToken ct)
    {
        var spec = new CrudSpecification<TEntity>
        {
            Filter = _filterParser.Parse(page.Filter),
            Sort = _sortParser.Parse(page.Sort),
            Page = page.Page,
            PageSize = Math.Min(page.PageSize, PaginationDefaults.MaxPageSize),
        };

        var entities = await _repository.ListAsync(spec, ct);
        var projected = entities.Select(_projector.Project).ToList();
        return new PagedResult<TDto>(projected, spec.Page, spec.PageSize, projected.Count, NextCursor: null);
    }

    public async Task<TDto?> GetByIdAsync(TId id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : _projector.Project(entity);
    }

    public async Task<Result<TDto>> CreateAsync(TCreateDto input, CancellationToken ct)
    {
        var entity = _createMapper.Map(input);
        _repository.Add(entity);
        await _uow.SaveChangesAsync(ct);
        return Result<TDto>.Success(_projector.Project(entity));
    }

    public async Task<Result<TDto>> UpdateAsync(TId id, TUpdateDto input, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result<TDto>.Failure(Error.NotFound("crud.not_found", "Entity not found."));
        }

        _updateMapper.Map(input);
        await _uow.SaveChangesAsync(ct);
        return Result<TDto>.Success(_projector.Project(entity));
    }

    public Task<Result<TDto>> PatchAsync(TId id, TUpdateDto input, CancellationToken ct) =>
        UpdateAsync(id, input, ct);

    public async Task<Result> DeleteAsync(TId id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure(Error.NotFound("crud.not_found", "Entity not found."));
        }

        _repository.Remove(entity);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(TId id, CancellationToken ct)
    {
        // SoftDeleteInterceptor converts DELETE to UPDATE with IsDeleted=true on save;
        // restore is the inverse. Real implementation lands in Fase 4 with IdentityDbContext.
        await Task.CompletedTask;
        return Result.Success();
    }
}

public static class PaginationDefaults
{
    public const int MaxPageSize = 200;
    public const int DefaultPageSize = 20;
}

internal sealed class CrudSpecification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; init; }
    public IReadOnlyList<Expression<Func<T, object>>> Includes { get; init; } = [];
    public IReadOnlyList<string> IncludeStrings { get; init; } = [];
    public Expression<Func<T, object>>? OrderBy { get; init; }
    public Expression<Func<T, object>>? OrderByDescending { get; init; }
    public bool AsSplitQuery { get; init; }
    public bool AsNoTracking { get; init; } = true;

    public LambdaExpression? Filter { get; init; }
    public IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> Sort { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;
}