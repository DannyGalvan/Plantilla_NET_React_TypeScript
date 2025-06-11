using FluentValidation.Results;
using Project.Server.Entities.Response;

namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Defines the <see>
    ///     <cref>IEntityService{TEntity, in TRequest, in TId}</cref>
    /// </see>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public interface IEntityService<TEntity, in TRequest, in TId>
    {
        /// <summary>
        /// The GetAll
        /// </summary>
        /// <param name="filters">The filters<see>
        ///         <cref>string?</cref>
        ///     </see>
        /// </param>
        /// <param name="includes">The includes<see>
        ///         <cref>string[]?</cref>
        ///     </see>
        /// </param>
        /// <param name="pageNumber">The pageNumber<see cref="int"/></param>
        /// <param name="pageSize">The pageSize<see cref="int"/></param>
        /// <param name="includeTotal">The pageSize<see cref="bool"/></param>
        /// <returns>The <see>
        ///         <cref>Response{List{TEntity}, List{ValidationFailure}}</cref>
        ///     </see>W
        /// </returns>
        Response<List<TEntity>, List<ValidationFailure>> GetAll(string? filters, string[]? includes = null, int pageNumber = 1, int pageSize = 30, bool includeTotal = false);

        /// <summary>
        /// The GetById
        /// </summary>
        /// <param name="id">The id<see cref="TId"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        Response<TEntity, List<ValidationFailure>> GetById(TId id);

        /// <summary>
        /// The Create
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        Response<TEntity, List<ValidationFailure>> Create(TRequest model);

        /// <summary>
        /// The Update
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        Response<TEntity, List<ValidationFailure>> Update(TRequest model);

        /// <summary>
        /// The PartialUpdate
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        Response<TEntity, List<ValidationFailure>> PartialUpdate(TRequest model);

        /// <summary>
        /// The Delete
        /// </summary>
        /// <param name="id">The id<see cref="TId"/></param>
        /// <param name="deletedBy">The deletedBy<see cref="long"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        Response<TEntity, List<ValidationFailure>> Delete(TId id, long deletedBy);
    }
}