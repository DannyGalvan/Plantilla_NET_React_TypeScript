using FluentValidation.Results;
using Project.Server.Entities.Response;

namespace Project.Server.Interceptors.Interfaces
{
    /// <summary>
    /// Runs before a logical delete. Receives the existing entity and the request
    /// (may carry the user id). Returning <c>Success = false</c> aborts the delete.
    /// </summary>
    public interface IEntityBeforeDeleteInterceptor<T, in TRequest>
    {
        Response<T, List<ValidationFailure>> Execute(Response<T, List<ValidationFailure>> response, TRequest request, T entity);
    }
}