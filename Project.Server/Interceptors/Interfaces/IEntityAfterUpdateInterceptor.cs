using FluentValidation.Results;
using Project.Server.Entities.Response;

namespace Project.Server.Interceptors.Interfaces
{
    /// <summary>
    /// Defines the <see cref="IEntityAfterUpdateInterceptor{T, in TRequest}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    public interface IEntityAfterUpdateInterceptor<T, in TRequest>
    {
        /// <summary>
        /// The Execute
        /// </summary>
        /// <param name="response">The response<see cref="Response{T, List{ValidationFailure}}"/></param>
        /// <param name="request">The request<see cref="TRequest"/></param>
        /// <param name="prevState">The prevState<see cref="T"/></param>
        /// <returns>The <see cref="Response{T, List{ValidationFailure}}"/></returns>
        Response<T, List<ValidationFailure>> Execute(Response<T, List<ValidationFailure>> response, TRequest request, T prevState);
    }
}
