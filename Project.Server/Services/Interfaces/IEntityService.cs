using FluentValidation.Results;
using Project.Server.Entities.Interfaces;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;

namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Generic CRUD service for any <see cref="IEntity{TIdEntity}"/>.
    ///
    /// Phase 1 contract:
    /// <list type="bullet">
    ///   <item>All methods are async and accept a <see cref="CancellationToken"/>.</item>
    ///   <item>Methods ending in <c>*Own</c> are scoped to the current user
    ///         (<see cref="IOwnedEntity{TId}"/> or <see cref="IEntity{TId}.CreatedBy"/>).</item>
    ///   <item><see cref="DeleteAsync"/> no longer takes <c>deletedBy</c>: the user
    ///         is resolved from the token.</item>
    ///   <item><c>GetAllWhitOutMetadata</c> was removed; it was a literal copy of
    ///         <c>GetAll</c> with the soft-delete filter dropped, which is
    ///         exactly what the B14 fix prevents.</item>
    /// </list>
    /// </summary>
    public interface IEntityService<TEntity, in TRequest, in TId>
        where TEntity : class, IEntity<TId>
        where TRequest : class
        where TId : struct
    {
        // -- General (admin) -------------------------------------------------

        Task<Response<List<TEntity>, List<ValidationFailure>>> GetAllAsync(
            QueryOptions options,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> GetByIdAsync(
            TId id,
            string[]? includes = null,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> CreateAsync(
            TRequest model,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> UpdateAsync(
            TRequest model,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> PartialUpdateAsync(
            TRequest model,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> DeleteAsync(
            TId id,
            CancellationToken ct = default);

        // -- Scoped to the current user -------------------------------------

        Task<Response<List<TEntity>, List<ValidationFailure>>> GetAllOwnAsync(
            QueryOptions options,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> GetByIdOwnAsync(
            TId id,
            string[]? includes = null,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> CreateOwnAsync(
            TRequest model,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> UpdateOwnAsync(
            TRequest model,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> PartialUpdateOwnAsync(
            TRequest model,
            CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> DeleteOwnAsync(
            TId id,
            CancellationToken ct = default);

        // -- Generic utilities ---------------------------------------------

        Task<Response<bool, List<ValidationFailure>>> ExistsAsync(TId id, CancellationToken ct = default);

        Task<Response<bool, List<ValidationFailure>>> IsOwnedByCurrentUserAsync(TId id, CancellationToken ct = default);

        Task<Response<int, List<ValidationFailure>>> CountAsync(string? filters = null, CancellationToken ct = default);

        Task<Response<int, List<ValidationFailure>>> CountOwnAsync(string? filters = null, CancellationToken ct = default);

        Task<Response<TEntity, List<ValidationFailure>>> RestoreAsync(TId id, CancellationToken ct = default);

        Task<Response<List<TEntity>, List<ValidationFailure>>> GetByOwnerAsync(
            long ownerId,
            QueryOptions options,
            CancellationToken ct = default);
    }
}