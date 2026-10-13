using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentValidation;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Querying;
using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Application.Crud;

/// <summary>
/// Default CRUD implementation for an aggregate. Tenant, organization and soft-delete isolation come from the
/// global filters of the module DbContext, so a row of another tenant or unit is simply "not found" (404).
/// </summary>
public sealed class CrudService<TEntity, TId, TDto, TCreate, TUpdate>(
    IRepository<TEntity, TId> repository,
    ICrudMapper<TEntity, TDto, TCreate, TUpdate> mapper,
    IQueryPolicy<TEntity> policy,
    QueryParser parser,
    ICurrentUser currentUser,
    IEnumerable<IValidator<TCreate>> createValidators,
    IEnumerable<IValidator<TUpdate>> updateValidators,
    IOptions<CrudOptions> options)
    : ICrudService<TId, TDto, TCreate, TUpdate>
    where TEntity : AggregateRoot<TId>
    where TId : notnull
    where TCreate : notnull
    where TUpdate : notnull
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string NotFoundCode = $"{typeof(TEntity).Name.ToLowerInvariant()}.not_found";

    public async Task<Result<PagedResult<TDto>>> ListAsync(PageRequest request, CrudScope scope, CancellationToken ct)
    {
        var filter = parser.ParseFilter(request.Filter, policy);
        if (filter.IsFailure)
        {
            return filter.Error!;
        }

        var sort = QueryParser.ParseSort(request.Sort, policy);
        if (sort.IsFailure)
        {
            return sort.Error!;
        }

        var page = request.NormalizedPage;
        var size = request.NormalizedPageSize(options.Value.MaxPageSize);

        var spec = BaseSpecification(scope);
        if (filter.Value is { } predicate)
        {
            spec.Where(predicate);
        }

        foreach (var (selector, descending) in sort.Value)
        {
            spec.OrderBy(selector, descending);
        }

        // Stable paging: always break ties by id.
        spec.OrderBy(IdSelector, sort.Value.Count > 0 && sort.Value[^1].Descending);

        long? total = request.IncludeTotal ? await repository.CountAsync(spec.WithoutPaging(), ct) : null;
        spec.Page((page - 1) * size, size);

        var items = await repository.ListAsync(spec, mapper.ProjectToDto, ct);
        return new PagedResult<TDto>(items, page, size, total);
    }

    public async Task<Result<TDto>> GetAsync(TId id, CrudScope scope, CancellationToken ct)
    {
        var items = await repository.ListAsync(BaseSpecification(scope).Where(ById(id)), mapper.ProjectToDto, ct);
        return items.Count == 0 ? Error.NotFound(NotFoundCode) : items[0];
    }

    public async Task<Result<TDto>> CreateAsync(TCreate input, CrudScope scope, CancellationToken ct)
    {
        var invalid = await ValidationRunner.ValidateAsync(createValidators, input, ct);
        if (invalid is not null)
        {
            return invalid;
        }

        var entity = mapper.ToEntity(input);
        if (scope == CrudScope.Owned && entity is IOwned owned)
        {
            owned.OwnerId = currentUser.UserId ?? throw new InvalidOperationException("Owned resources require a user.");
        }

        repository.Add(entity);
        var saved = await SaveAsync(ct);
        return saved.IsFailure ? saved.Error! : mapper.ToDto(entity);
    }

    public async Task<Result<TDto>> UpdateAsync(TId id, TUpdate input, CrudScope scope, CancellationToken ct)
    {
        var entity = await LoadTrackedAsync(id, scope, ct);
        if (entity is null)
        {
            return Error.NotFound(NotFoundCode);
        }

        return await ApplyUpdateAsync(entity, input, ct);
    }

    public async Task<Result<TDto>> PatchAsync(TId id, JsonObject patch, CrudScope scope, CancellationToken ct)
    {
        var entity = await LoadTrackedAsync(id, scope, ct);
        if (entity is null)
        {
            return Error.NotFound(NotFoundCode);
        }

        TUpdate merged;
        try
        {
            var current = JsonSerializer.SerializeToNode(mapper.ToUpdate(entity), JsonOptions)!.AsObject();
            JsonMergePatch.Apply(current, patch);
            merged = current.Deserialize<TUpdate>(JsonOptions)
                ?? throw new JsonException("The patch produced an empty document.");
        }
        catch (JsonException)
        {
            return Error.Validation("body", "The patch document is not valid for this resource.");
        }

        return await ApplyUpdateAsync(entity, merged, ct);
    }

    public async Task<Result> DeleteAsync(TId id, CrudScope scope, CancellationToken ct)
    {
        var entity = await LoadTrackedAsync(id, scope, ct);
        if (entity is null)
        {
            return Error.NotFound(NotFoundCode);
        }

        repository.Remove(entity);
        return await SaveAsync(ct);
    }

    public async Task<Result> RestoreAsync(TId id, CrudScope scope, CancellationToken ct)
    {
        var spec = BaseSpecification(scope).Where(ById(id)).WithDeleted().Tracked();
        var entity = await repository.FirstOrDefaultAsync(spec, ct);
        if (entity is not ISoftDeletable { IsDeleted: true } deleted)
        {
            return Error.NotFound(NotFoundCode);
        }

        deleted.IsDeleted = false;
        deleted.DeletedAt = null;
        deleted.DeletedBy = null;
        return await SaveAsync(ct);
    }

    private async Task<Result<TDto>> ApplyUpdateAsync(TEntity entity, TUpdate input, CancellationToken ct)
    {
        var invalid = await ValidationRunner.ValidateAsync(updateValidators, input, ct);
        if (invalid is not null)
        {
            return invalid;
        }

        if (input is IConcurrencyAware { ConcurrencyStamp: { } expected }
            && entity is IHasConcurrencyStamp stamped
            && stamped.ConcurrencyStamp != expected)
        {
            return ConcurrencyError();
        }

        mapper.Apply(input, entity);
        var saved = await SaveAsync(ct);
        return saved.IsFailure ? saved.Error! : mapper.ToDto(entity);
    }

    public async Task<Result<ImportReport>> ImportAsync(IReadOnlyList<ImportRow<TCreate>> rows, bool dryRun, CancellationToken ct)
    {
        var errors = new List<ImportRowError>();
        foreach (var row in rows)
        {
            var problems = row.Errors.ToList();
            if (row.Value is not null && await ValidationRunner.ValidateAsync(createValidators, row.Value, ct) is { } invalid)
            {
                problems.AddRange(invalid.Details?.SelectMany(d => d.Value.Select(m => $"{d.Key}: {m}")) ?? [invalid.Description]);
            }

            if (problems.Count > 0)
            {
                errors.Add(new ImportRowError(row.Number, problems));
            }
        }

        if (errors.Count > 0 || dryRun)
        {
            return new ImportReport(rows.Count, 0, dryRun, errors);
        }

        foreach (var row in rows)
        {
            repository.Add(mapper.ToEntity(row.Value!));
        }

        var saved = await SaveAsync(ct);
        return saved.IsFailure ? saved.Error! : new ImportReport(rows.Count, rows.Count, false, []);
    }

    private async Task<Result> SaveAsync(CancellationToken ct)
    {
        try
        {
            await repository.UnitOfWork.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (ConcurrencyConflictException)
        {
            return ConcurrencyError();
        }
        catch (UniqueConstraintException)
        {
            return Error.Conflict($"{typeof(TEntity).Name.ToLowerInvariant()}.duplicate", "A record with the same unique values already exists.");
        }
    }

    private Task<TEntity?> LoadTrackedAsync(TId id, CrudScope scope, CancellationToken ct) =>
        repository.FirstOrDefaultAsync(BaseSpecification(scope).Where(ById(id)).Tracked(), ct);

    private Specification<TEntity> BaseSpecification(CrudScope scope)
    {
        var spec = new Specification<TEntity>();
        if (scope == CrudScope.Owned)
        {
            if (!typeof(IOwned).IsAssignableFrom(typeof(TEntity)))
            {
                throw new InvalidOperationException($"{typeof(TEntity).Name} does not implement IOwned.");
            }

            spec.Where(OwnedBy(currentUser.UserId ?? string.Empty));
        }

        return spec;
    }

    private static Error ConcurrencyError() =>
        Error.Conflict("concurrency.conflict", "The resource was modified by someone else. Reload it and try again.");

    private static readonly Expression<Func<TEntity, TId>> IdSelector = e => e.Id;

    private static Expression<Func<TEntity, bool>> ById(TId id)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var body = Expression.Equal(
            Expression.Property(parameter, nameof(Entity<TId>.Id)),
            FilterValue.Parameter(id, typeof(TId)));
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static Expression<Func<TEntity, bool>> OwnedBy(string userId)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var body = Expression.Equal(
            Expression.Property(parameter, nameof(IOwned.OwnerId)),
            FilterValue.Parameter(userId, typeof(string)));
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }
}

/// <summary>RFC 7386 JSON Merge Patch.</summary>
public static class JsonMergePatch
{
    public static void Apply(JsonObject target, JsonObject patch)
    {
        foreach (var (name, value) in patch.ToList())
        {
            var key = target.ContainsKey(name)
                ? name
                : target.Select(p => p.Key).FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)) ?? name;

            if (value is JsonObject nestedPatch && target[key] is JsonObject nestedTarget)
            {
                Apply(nestedTarget, nestedPatch);
            }
            else
            {
                target[key] = value?.DeepClone();
            }
        }
    }
}
