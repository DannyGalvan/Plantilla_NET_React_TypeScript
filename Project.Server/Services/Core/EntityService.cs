using System.Diagnostics;
using System.Linq.Expressions;
using FluentValidation.Results;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Project.Server.Configs.Models;
using Project.Server.Context;
using Project.Server.Entities.Interfaces;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Services.Core
{
    /// <summary>
    /// Generic CRUD service. Phase 1 rewrite:
    /// <list type="bullet">
    ///   <item>Async throughout with <see cref="CancellationToken"/> propagation.</item>
    ///   <item>Filters, sort and includes go through <see cref="IQueryPolicy"/> —
    ///         sensitive columns are unreachable from the query string (closes B3).</item>
    ///   <item>Soft-delete is handled by a global EF query filter, so
    ///         <c>GetById</c>, <c>Update</c>, <c>PartialUpdate</c> and the count
    ///         variants all stop returning logical-deleted rows automatically
    ///         (closes B14).</item>
    ///   <item>Pagination is clamped via <see cref="PaginationOptions"/>
    ///         (closes B13).</item>
    ///   <item>Exceptions never leak through <c>response.Message</c>; the client
    ///         sees a generic message and a trace id (closes B11).</item>
    ///   <item>Mass assignment hardened via <see cref="Util.MassAssignmentDenylist"/>
    ///         (closes B2).</item>
    /// </list>
    /// </summary>
    public partial class EntityService<TEntity, TRequest, TId> : IEntityService<TEntity, TRequest, TId>
        where TEntity : class, IEntity<TId>
        where TRequest : class
        where TId : struct
    {
        private readonly IMapper _mapper;
        private readonly ILogger<EntityService<TEntity, TRequest, TId>> _logger;
        private readonly DataContext _db;
        private readonly IFilterTranslator _filterTranslator;
        private readonly IEntitySupportService _entitySupportService;
        private readonly ICurrentUserService _currentUser;
        private readonly QueryPolicy<TEntity> _queryPolicy;
        private readonly ISortTranslator _sortTranslator;
        private readonly PaginationOptions _pagination;

        public EntityService(
            IMapper mapper,
            ILogger<EntityService<TEntity, TRequest, TId>> logger,
            DataContext db,
            IFilterTranslator filterTranslator,
            IEntitySupportService entitySupportService,
            ICurrentUserService currentUser,
            QueryPolicy<TEntity> queryPolicy,
            ISortTranslator sortTranslator,
            PaginationOptions pagination)
        {
            _mapper = mapper;
            _logger = logger;
            _db = db;
            _filterTranslator = filterTranslator;
            _entitySupportService = entitySupportService;
            _currentUser = currentUser;
            _queryPolicy = queryPolicy;
            _sortTranslator = sortTranslator;
            _pagination = pagination;
        }

        // ================================================================
        // General
        // ================================================================

        public async Task<Response<List<TEntity>, List<ValidationFailure>>> GetAllAsync(
            QueryOptions options, CancellationToken ct = default)
        {
            try
            {
                var (query, filterError) = BuildQuery(options, ownedBy: null);
                if (filterError is not null)
                    return ValidationFailed<List<TEntity>>(filterError.PropertyName, filterError.ErrorMessage);

                int pageSize = _pagination.ClampPageSize(options.PageSize);
                int pageNumber = _pagination.ClampPageNumber(options.PageNumber);
                int skip = (pageNumber - 1) * pageSize;

                var paged = await query.Skip(skip).Take(pageSize + 1).AsNoTracking().ToListAsync(ct);

                var response = NewOk<List<TEntity>>();
                response.Data = paged.Take(pageSize).ToList();
                response.TotalResults = options.IncludeTotal
                    ? await query.CountAsync(ct)
                    : skip + response.Data.Count + (paged.Count > pageSize ? 1 : 0);
                response.Message = $"Entities {typeof(TEntity).Name} retrieved successfully";
                return response;
            }
            catch (Exception ex)
            {
                return Handle<List<TEntity>>(ex, "GetAll");
            }
        }

        public async Task<Response<TEntity, List<ValidationFailure>>> GetByIdAsync(
            TId id, string[]? includes = null, CancellationToken ct = default)
        {
            try
            {
                var query = _db.Set<TEntity>().AsNoTracking().AsQueryable();
                query = ApplyIncludes(query, includes);
                var entity = await query.FirstOrDefaultAsync(BuildIdEquality(id), ct);
                if (entity is null) return NotFound<TEntity>("GetById");
                var response = NewOk<TEntity>();
                response.Data = entity;
                response.Message = $"Entity {typeof(TEntity).Name} retrieved successfully";
                return response;
            }
            catch (Exception ex)
            {
                return Handle<TEntity>(ex, "GetById");
            }
        }

        public Task<Response<TEntity, List<ValidationFailure>>> CreateAsync(TRequest model, CancellationToken ct = default)
            => CreateInternalAsync(model, ownedBy: null, ct);

        public Task<Response<TEntity, List<ValidationFailure>>> UpdateAsync(TRequest model, CancellationToken ct = default)
            => UpdateInternalAsync(model, beforePartial: false, requireOwned: false, ct);

        public Task<Response<TEntity, List<ValidationFailure>>> PartialUpdateAsync(TRequest model, CancellationToken ct = default)
            => UpdateInternalAsync(model, beforePartial: true, requireOwned: false, ct);

        public async Task<Response<TEntity, List<ValidationFailure>>> DeleteAsync(TId id, CancellationToken ct = default)
        {
            try
            {
                long userId;
                try { userId = _currentUser.UserId; }
                catch (UnauthorizedAccessException) { return Forbidden<TEntity>("Delete"); }

                if (!Util.HasValidId(id)) return ValidationFailed<TEntity>("Id", "Invalid Id");

                var entity = await _db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(BuildIdEquality(id), ct);
                if (entity is null) return NotFound<TEntity>("Delete");

                var response = NewOk<TEntity>();
                foreach (var interceptor in _entitySupportService.GetBeforeDeleteInterceptors<TEntity, TRequest>())
                {
                    if (!response.Success) return response;
                    response = interceptor.Execute(response, default!, entity);
                }

                entity.UpdatedAt = DateTime.UtcNow;
                entity.State = 0;
                entity.UpdatedBy = userId;

                _db.Set<TEntity>().Update(entity);
                await _db.SaveChangesAsync(ct);

                response.Data = entity;
                response.Message = $"Entity {typeof(TEntity).Name} deleted successfully";

                foreach (var interceptor in _entitySupportService.GetAfterDeleteInterceptors<TEntity, TRequest>())
                {
                    if (!response.Success) return response;
                    response = interceptor.Execute(response, default!, entity);
                }

                return response;
            }
            catch (Exception ex)
            {
                return Handle<TEntity>(ex, "Delete");
            }
        }

        // ================================================================
        // Owned
        // ================================================================

        public Task<Response<List<TEntity>, List<ValidationFailure>>> GetAllOwnAsync(
            QueryOptions options, CancellationToken ct = default)
        {
            return GetAllInternalAsync(options, requireOwned: true, overrideOwnerId: null, ct);
        }

        public async Task<Response<TEntity, List<ValidationFailure>>> GetByIdOwnAsync(
            TId id, string[]? includes = null, CancellationToken ct = default)
        {
            try
            {
                long userId;
                try { userId = _currentUser.UserId; }
                catch (UnauthorizedAccessException) { return Forbidden<TEntity>("GetByIdOwn"); }

                var query = _db.Set<TEntity>().AsNoTracking().AsQueryable();
                query = ApplyIncludes(query, includes);
                var entity = await query.FirstOrDefaultAsync(
                    Combine(BuildIdEquality(id), OwnershipResolver.Predicate<TEntity>(userId)), ct);

                if (entity is null)
                {
                    var exists = await _db.Set<TEntity>().AsNoTracking().AnyAsync(BuildIdEquality(id), ct);
                    return exists ? Forbidden<TEntity>("GetByIdOwn") : NotFound<TEntity>("GetByIdOwn");
                }
                var response = NewOk<TEntity>();
                response.Data = entity;
                response.Message = $"Entity {typeof(TEntity).Name} retrieved successfully";
                return response;
            }
            catch (Exception ex)
            {
                return Handle<TEntity>(ex, "GetByIdOwn");
            }
        }

        public Task<Response<TEntity, List<ValidationFailure>>> CreateOwnAsync(TRequest model, CancellationToken ct = default)
        {
            try
            {
                long userId = _currentUser.UserId;
                return CreateInternalAsync(model, ownedBy: userId, ct);
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult(Forbidden<TEntity>("CreateOwn"));
            }
        }

        public Task<Response<TEntity, List<ValidationFailure>>> UpdateOwnAsync(TRequest model, CancellationToken ct = default)
            => UpdateInternalAsync(model, beforePartial: false, requireOwned: true, ct);

        public Task<Response<TEntity, List<ValidationFailure>>> PartialUpdateOwnAsync(TRequest model, CancellationToken ct = default)
            => UpdateInternalAsync(model, beforePartial: true, requireOwned: true, ct);

        public async Task<Response<TEntity, List<ValidationFailure>>> DeleteOwnAsync(TId id, CancellationToken ct = default)
        {
            try
            {
                long userId;
                try { userId = _currentUser.UserId; }
                catch (UnauthorizedAccessException) { return Forbidden<TEntity>("DeleteOwn"); }

                if (!Util.HasValidId(id)) return ValidationFailed<TEntity>("Id", "Invalid Id");

                var entity = await _db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(
                    Combine(BuildIdEquality(id), OwnershipResolver.Predicate<TEntity>(userId)), ct);
                if (entity is null)
                {
                    var exists = await _db.Set<TEntity>().AsNoTracking().AnyAsync(BuildIdEquality(id), ct);
                    return exists ? Forbidden<TEntity>("DeleteOwn") : NotFound<TEntity>("DeleteOwn");
                }

                var response = NewOk<TEntity>();
                foreach (var interceptor in _entitySupportService.GetBeforeDeleteInterceptors<TEntity, TRequest>())
                {
                    if (!response.Success) return response;
                    response = interceptor.Execute(response, default!, entity);
                }

                entity.UpdatedAt = DateTime.UtcNow;
                entity.State = 0;
                entity.UpdatedBy = userId;

                _db.Set<TEntity>().Update(entity);
                await _db.SaveChangesAsync(ct);

                response.Data = entity;
                response.Message = $"Entity {typeof(TEntity).Name} deleted successfully";

                foreach (var interceptor in _entitySupportService.GetAfterDeleteInterceptors<TEntity, TRequest>())
                {
                    if (!response.Success) return response;
                    response = interceptor.Execute(response, default!, entity);
                }

                return response;
            }
            catch (Exception ex)
            {
                return Handle<TEntity>(ex, "DeleteOwn");
            }
        }

        // ================================================================
        // Utilities
        // ================================================================

        public async Task<Response<bool, List<ValidationFailure>>> ExistsAsync(TId id, CancellationToken ct = default)
        {
            try
            {
                var response = NewOk<bool>();
                response.Data = await _db.Set<TEntity>().AsNoTracking().AnyAsync(BuildIdEquality(id), ct);
                response.Message = response.Data ? "Exists" : "Not found";
                return response;
            }
            catch (Exception ex) { return Handle<bool>(ex, "Exists"); }
        }

        public async Task<Response<bool, List<ValidationFailure>>> IsOwnedByCurrentUserAsync(TId id, CancellationToken ct = default)
        {
            try
            {
                long userId;
                try { userId = _currentUser.UserId; }
                catch (UnauthorizedAccessException) { return Forbidden<bool>("IsOwnedByCurrentUser"); }

                var response = NewOk<bool>();
                response.Data = await _db.Set<TEntity>().AsNoTracking().AnyAsync(
                    Combine(BuildIdEquality(id), OwnershipResolver.Predicate<TEntity>(userId)), ct);
                response.Message = response.Data ? "Owned by current user" : "Not owned";
                return response;
            }
            catch (Exception ex) { return Handle<bool>(ex, "IsOwned"); }
        }

        public async Task<Response<int, List<ValidationFailure>>> CountAsync(string? filters = null, CancellationToken ct = default)
        {
            try
            {
                var query = _db.Set<TEntity>().AsNoTracking().AsQueryable();
                query = ApplyFilters(query, filters, out var filterError);
                if (filterError is not null) return ValidationFailed<int>(filterError.PropertyName, filterError.ErrorMessage);

                var response = NewOk<int>();
                response.Data = await query.CountAsync(ct);
                response.Message = $"Count = {response.Data}";
                return response;
            }
            catch (Exception ex) { return Handle<int>(ex, "Count"); }
        }

        public async Task<Response<int, List<ValidationFailure>>> CountOwnAsync(string? filters = null, CancellationToken ct = default)
        {
            try
            {
                long userId;
                try { userId = _currentUser.UserId; }
                catch (UnauthorizedAccessException) { return Forbidden<int>("CountOwn"); }

                var query = _db.Set<TEntity>().AsNoTracking().AsQueryable();
                query = ApplyFilters(query, filters, out var filterError);
                if (filterError is not null) return ValidationFailed<int>(filterError.PropertyName, filterError.ErrorMessage);

                query = query.Where(OwnershipResolver.Predicate<TEntity>(userId));
                var response = NewOk<int>();
                response.Data = await query.CountAsync(ct);
                response.Message = $"Count = {response.Data}";
                return response;
            }
            catch (Exception ex) { return Handle<int>(ex, "CountOwn"); }
        }

        public async Task<Response<TEntity, List<ValidationFailure>>> RestoreAsync(TId id, CancellationToken ct = default)
        {
            try
            {
                long userId;
                try { userId = _currentUser.UserId; }
                catch (UnauthorizedAccessException) { return Forbidden<TEntity>("Restore"); }

                if (!Util.HasValidId(id)) return ValidationFailed<TEntity>("Id", "Invalid Id");

                var entity = await _db.Set<TEntity>().IgnoreQueryFilters()
                    .AsNoTracking().FirstOrDefaultAsync(BuildIdEquality(id), ct);
                if (entity is null) return NotFound<TEntity>("Restore");

                entity.State = 1;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = userId;

                _db.Set<TEntity>().Update(entity);
                await _db.SaveChangesAsync(ct);

                var response = NewOk<TEntity>();
                response.Data = entity;
                response.Message = $"Entity {typeof(TEntity).Name} restored successfully";
                return response;
            }
            catch (Exception ex) { return Handle<TEntity>(ex, "Restore"); }
        }

        public Task<Response<List<TEntity>, List<ValidationFailure>>> GetByOwnerAsync(
            long ownerId, QueryOptions options, CancellationToken ct = default)
        {
            return GetAllInternalAsync(options, requireOwned: true, overrideOwnerId: ownerId, ct);
        }

        // ================================================================
        // Internals
        // ================================================================

        private async Task<Response<List<TEntity>, List<ValidationFailure>>> GetAllInternalAsync(
            QueryOptions options, bool requireOwned, long? overrideOwnerId, CancellationToken ct)
        {
            try
            {
                long? ownedBy = null;
                if (requireOwned)
                {
                    try { ownedBy = overrideOwnerId ?? _currentUser.UserId; }
                    catch (UnauthorizedAccessException) { return Forbidden<List<TEntity>>("GetAllOwn"); }
                }

                var (query, filterError) = BuildQuery(options, ownedBy);
                if (filterError is not null)
                    return ValidationFailed<List<TEntity>>(filterError.PropertyName, filterError.ErrorMessage);

                int pageSize = _pagination.ClampPageSize(options.PageSize);
                int pageNumber = _pagination.ClampPageNumber(options.PageNumber);
                int skip = (pageNumber - 1) * pageSize;

                var paged = await query.Skip(skip).Take(pageSize + 1).AsNoTracking().ToListAsync(ct);

                var response = NewOk<List<TEntity>>();
                response.Data = paged.Take(pageSize).ToList();
                response.TotalResults = options.IncludeTotal
                    ? await query.CountAsync(ct)
                    : skip + response.Data.Count + (paged.Count > pageSize ? 1 : 0);
                response.Message = $"Entities {typeof(TEntity).Name} retrieved successfully";
                return response;
            }
            catch (Exception ex)
            {
                return Handle<List<TEntity>>(ex, "GetAll");
            }
        }

        private (IQueryable<TEntity> Query, ValidationFailure? FilterError) BuildQuery(QueryOptions options, long? ownedBy)
        {
            var query = _db.Set<TEntity>().AsNoTracking().AsQueryable();

            if (options.IncludeDeleted)
            {
                // admin-only flag: callers must gate via [RequireOperation]
                query = query.IgnoreQueryFilters();
            }

            query = ApplyIncludes(query, options.Includes);
            query = ApplyFilters(query, options.Filters, out var filterError);

            if (filterError is not null)
            {
                return (query, filterError);
            }

            if (ownedBy is not null)
            {
                query = query.Where(OwnershipResolver.Predicate<TEntity>(ownedBy.Value));
            }

            query = ApplySort(query, options.SortBy, options.SortDescending);
            return (query, null);
        }

        private IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query, string[]? includes)
        {
            if (includes is null || includes.Length == 0) return query;
            // Depth/quantity caps are enforced inside ApplyIncludes — invalid paths
            // throw, which the outer try/catch converts into a 400.
            return query.ApplyIncludes(includes);
        }

        private IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, string? filters, out ValidationFailure? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(filters)) return query;

            try
            {
                // Verify every property touched by the filter is in the allowlist.
                foreach (var referenced in ExtractPropertyNames(filters))
                {
                    if (!_queryPolicy.IsFilterable(referenced))
                    {
                        error = new ValidationFailure(referenced, $"Property '{referenced}' is not filterable.");
                        return query;
                    }
                }
                var translated = _filterTranslator.TranslateToEfFilter<TEntity>(filters);
                return query.Where(translated);
            }
            catch (Exception ex)
            {
                error = new ValidationFailure("Filters", ex.Message);
                return query;
            }
        }

        private IQueryable<TEntity> ApplySort(IQueryable<TEntity> query, string? sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy)) return OrderByCreatedAtDesc(query);

            if (!_sortTranslator.TryBuild<TEntity>(sortBy, _queryPolicy.IsSortable, out var keySelector) || keySelector is null)
            {
                return OrderByCreatedAtDesc(query);
            }

            var methodName = descending ? "OrderByDescending" : "OrderBy";
            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), keySelector.ReturnType);
            return (IOrderedQueryable<TEntity>)method.Invoke(null, new object?[] { query, keySelector })!;
        }

        private static IOrderedQueryable<TEntity> OrderByCreatedAtDesc(IQueryable<TEntity> query)
            => query.OrderByDescending(e => e.CreatedAt);

        private async Task<Response<TEntity, List<ValidationFailure>>> CreateInternalAsync(
            TRequest model, long? ownedBy, CancellationToken ct)
        {
            try
            {
                var results = _entitySupportService.GetValidator<TRequest>("Create").Validate(model);
                if (!results.IsValid)
                {
                    var v = NewOk<TEntity>();
                    v.Status = ResponseStatus.ValidationFailed;
                    v.Success = false;
                    v.Message = "Validation failed";
                    v.Errors = results.Errors;
                    return v;
                }

                var entity = _mapper.Map<TEntity>(model!);

                // Force ownership fields from the token (B1 — CreateOwn).
                if (ownedBy is not null)
                {
                    if (entity is IOwnedEntity<long> owned) owned.OwnerId = ownedBy.Value;
                    entity.CreatedBy = ownedBy.Value;
                }

                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = null;
                entity.UpdatedBy = null;
                entity.State = entity.State == 0 ? 1 : entity.State;

                var response = NewOk<TEntity>();
                response.Data = entity;
                foreach (var interceptor in _entitySupportService.GetBeforeCreateInterceptors<TEntity, TRequest>())
                {
                    if (!response.Success) return response;
                    response = interceptor.Execute(response, model);
                    entity = response.Data!;
                }

                if (!response.Success) return response;

                _db.Set<TEntity>().Add(entity);
                await _db.SaveChangesAsync(ct);

                response.Data = entity;
                response.Message = $"Entity {typeof(TEntity).Name} created successfully";

                foreach (var interceptor in _entitySupportService.GetAfterCreateInterceptors<TEntity, TRequest>())
                {
                    if (!response.Success) return response;
                    response = interceptor.Execute(response, model);
                }

                return response;
            }
            catch (Exception ex) { return Handle<TEntity>(ex, "Create"); }
        }

        private async Task<Response<TEntity, List<ValidationFailure>>> UpdateInternalAsync(
            TRequest model, bool beforePartial, bool requireOwned, CancellationToken ct)
        {
            try
            {
                long userId = 0;
                if (requireOwned)
                {
                    try { userId = _currentUser.UserId; }
                    catch (UnauthorizedAccessException) { return Forbidden<TEntity>("Update"); }
                }

                var validatorKey = beforePartial ? "Partial" : "Update";
                var results = _entitySupportService.GetValidator<TRequest>(validatorKey).Validate(model);
                if (!results.IsValid)
                {
                    var v = NewOk<TEntity>();
                    v.Status = ResponseStatus.ValidationFailed;
                    v.Success = false;
                    v.Message = "Validation failed";
                    v.Errors = results.Errors;
                    return v;
                }

                TEntity mapped = _mapper.Map<TEntity>(model!);
                var database = _db.Set<TEntity>();

                var prevState = await database.AsNoTracking().FirstOrDefaultAsync(BuildIdEquality(mapped.Id), ct);
                if (prevState is null) return NotFound<TEntity>("Update");

                if (requireOwned)
                {
                    if (!await database.AsNoTracking().AnyAsync(
                        Combine(BuildIdEquality(mapped.Id), OwnershipResolver.Predicate<TEntity>(userId)), ct))
                    {
                        return Forbidden<TEntity>("Update");
                    }
                }

                var entityToUpdate = _mapper.Map<TEntity>(prevState);
                var createdAt = entityToUpdate.CreatedAt;

                Util.UpdateProperties(entityToUpdate, mapped);
                entityToUpdate.UpdatedAt = DateTime.UtcNow;
                entityToUpdate.CreatedAt = createdAt;

                var response = NewOk<TEntity>();
                response.Data = entityToUpdate;
                if (beforePartial)
                {
                    foreach (var interceptor in _entitySupportService.GetBeforePartialUpdateInterceptors<TEntity, TRequest>())
                    {
                        if (!response.Success) return response;
                        response = interceptor.Execute(response, model, entityToUpdate);
                        entityToUpdate = response.Data!;
                    }
                }
                else
                {
                    foreach (var interceptor in _entitySupportService.GetBeforeUpdateInterceptors<TEntity, TRequest>())
                    {
                        if (!response.Success) return response;
                        response = interceptor.Execute(response, model);
                        entityToUpdate = response.Data!;
                    }
                }

                if (!response.Success) return response;

                // Clear any tracked snapshot the InMemory provider may have left
                // behind — even though we asked for AsNoTracking, the provider
                // can leave a phantom entry that collides with the attach below.
                _db.ChangeTracker.Clear();

                database.Update(entityToUpdate);
                await _db.SaveChangesAsync(ct);

                response.Data = entityToUpdate;
                response.Message = $"Entity {typeof(TEntity).Name} updated successfully";

                var afterInterceptors = _entitySupportService.GetAfterUpdateInterceptors<TEntity, TRequest>();
                foreach (var interceptor in afterInterceptors)
                {
                    if (!response.Success) return response;
                    response = interceptor.Execute(response, model, prevState);
                }

                return response;
            }
            catch (Exception ex) { return Handle<TEntity>(ex, "Update"); }
        }

        // Sentinel marker — closing brace intentionally kept below.
        // (no other members between here and the helpers block)

        // ================================================================
        // Helpers
        // ================================================================

        private static Expression<Func<TEntity, bool>> BuildIdEquality(TId id)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var member = Expression.PropertyOrField(parameter, "Id");
            var constant = Expression.Constant(id, member.Type);
            var body = Expression.Equal(member, constant);
            return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        }

        private static Expression<Func<TEntity, bool>> Combine(
            Expression<Func<TEntity, bool>> first,
            Expression<Func<TEntity, bool>> second)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var body = Expression.AndAlso(
                Expression.Invoke(first, parameter),
                Expression.Invoke(second, parameter));
            return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        }

        private static IEnumerable<string> ExtractPropertyNames(string filters)
        {
            foreach (var segment in filters.Split([" AND ", " OR "], StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = segment.Split(':');
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    yield return parts[0].Trim();
                }
            }
        }

        private static Response<TData, List<ValidationFailure>> NewOk<TData>()
            => new() { Success = true, Status = ResponseStatus.Ok };

        private static Response<TData, List<ValidationFailure>> ValidationFailed<TData>(string field, string msg)
            => new()
            {
                Success = false,
                Status = ResponseStatus.ValidationFailed,
                Message = "Validation failed",
                Errors = new List<ValidationFailure> { new(field, msg) },
            };

        private static Response<TData, List<ValidationFailure>> NotFound<TData>(string op)
            => new()
            {
                Success = false,
                Status = ResponseStatus.NotFound,
                Message = $"Entity {typeof(TEntity).Name} not found",
            };

        private static Response<TData, List<ValidationFailure>> Forbidden<TData>(string op)
            => new()
            {
                Success = false,
                Status = ResponseStatus.Forbidden,
                Message = "Access denied",
            };

        private Response<TData, List<ValidationFailure>> Handle<TData>(Exception ex, string op)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
            _logger.LogError(ex, "{operation} on {entity} failed (traceId={traceId})", op, typeof(TEntity).Name, traceId);
            return new()
            {
                Success = false,
                Status = ResponseStatus.Error,
                Message = $"Operation failed (traceId={traceId}).",
            };
        }
    }
}