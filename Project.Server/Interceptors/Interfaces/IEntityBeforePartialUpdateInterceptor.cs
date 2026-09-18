using FluentValidation.Results;
using Project.Server.Entities.Response;

namespace Project.Server.Interceptors.Interfaces
{
    /// <summary>
    /// Runs before a partial update. Until now the generic stack reused the
    /// <c>BeforeUpdate</c> hook for partial updates; this dedicated hook lets
    /// callers distinguish between full and partial writes.
    /// </summary>
    public interface IEntityBeforePartialUpdateInterceptor<T, in TRequest>
    {
        Response<T, List<ValidationFailure>> Execute(Response<T, List<ValidationFailure>> response, TRequest request, T entity);
    }
}