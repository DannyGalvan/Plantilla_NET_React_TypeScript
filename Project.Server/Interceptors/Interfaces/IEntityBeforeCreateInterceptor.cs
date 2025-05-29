using FluentValidation.Results;
using Project.Server.Entities.Response;

namespace Project.Server.Interceptors.Interfaces
{
    /// <summary>
    /// Defines the <see cref="IEntityBeforeCreateInterceptor{T, in TRequest}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    public interface IEntityBeforeCreateInterceptor<T, in TRequest>
    {
        /// <summary>
        /// The Execute
        /// </summary>
        /// <param name="response">The response<see cref="Response{T, List{ValidationFailure}}"/></param>
        /// <param name="request">The request<see cref="TRequest"/></param>
        /// <returns>The <see cref="Response{T, List{ValidationFailure}}"/></returns>
        Response<T, List<ValidationFailure>> Execute(Response<T, List<ValidationFailure>> response, TRequest request);
    }
}
