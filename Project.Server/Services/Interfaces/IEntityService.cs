using FluentValidation.Results;
using Project.Server.Entities.Response;

namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Defines the <see cref="IEntityService{TEntity, in TRequest, in TId}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public interface IEntityService<TEntity, in TRequest, in TId>
    {
        /// <summary>
        /// The GetAll
        /// </summary>
        /// <param name="filters">The filters<see cref="string?"/></param>
        /// <param name="thenInclude">The thenInclude<see cref="bool?"/></param>
        /// <param name="pageNumber">The pageNumber<see cref="int"/></param>
        /// <param name="pageSize">The pageSize<see cref="int"/></param>
        /// <returns>The <see cref="Response{List{TEntity}, List{ValidationFailure}}"/></returns>
        Response<List<TEntity>, List<ValidationFailure>> GetAll(string? filters, bool? thenInclude = false, int pageNumber = 1, int pageSize = 30);

        /// <summary>
        /// The GetById
        /// </summary>
        /// <param name="id">The id<see cref="TId"/></param>
        /// <returns>The <see cref="Response{TEntity, List{ValidationFailure}}"/></returns>
        Response<TEntity, List<ValidationFailure>> GetById(TId id);

        /// <summary>
        /// The Create
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see cref="Response{TEntity, List{ValidationFailure}}"/></returns>
        Response<TEntity, List<ValidationFailure>> Create(TRequest model);

        /// <summary>
        /// The Update
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see cref="Response{TEntity, List{ValidationFailure}}"/></returns>
        Response<TEntity, List<ValidationFailure>> Update(TRequest model);

        /// <summary>
        /// The PartialUpdate
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see cref="Response{TEntity, List{ValidationFailure}}"/></returns>
        Response<TEntity, List<ValidationFailure>> PartialUpdate(TRequest model);

        /// <summary>
        /// The Delete
        /// </summary>
        /// <param name="id">The id<see cref="TId"/></param>
        /// <param name="deletedBy">The deletedBy<see cref="long"/></param>
        /// <returns>The <see cref="Response{TEntity, List{ValidationFailure}}"/></returns>
        Response<TEntity, List<ValidationFailure>> Delete(TId id, long deletedBy);
    }
}
