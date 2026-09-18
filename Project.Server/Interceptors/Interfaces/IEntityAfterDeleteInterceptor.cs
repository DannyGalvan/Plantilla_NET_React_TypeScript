using FluentValidation.Results;
using Project.Server.Entities.Response;

namespace Project.Server.Interceptors.Interfaces
{
    /// <summary>
    /// Runs after a logical delete. Receives the soft-deleted entity and the request.
    /// </summary>
    public interface IEntityAfterDeleteInterceptor<T, in TRequest>
    {
        Response<T, List<ValidationFailure>> Execute(Response<T, List<ValidationFailure>> response, TRequest request, T entity);
    }
}