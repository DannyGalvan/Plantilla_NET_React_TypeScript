using FluentValidation.Results;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Interceptors.Interfaces;
using BC = BCrypt.Net;

namespace Project.Server.Interceptors.UserInterceptors
{
    /// <summary>
    /// Defines the <see cref="UserBeforeUpdateInterceptor" />
    /// </summary>
    public class UserBeforeUpdateInterceptor : IEntityBeforeUpdateInterceptor<User, UserRequest>
    {
        /// <summary>
        /// The Execute
        /// </summary>
        /// <param name="response">The response<see cref="Response{User, List{ValidationFailure}}"/></param>
        /// <param name="request">The request<see cref="UserRequest"/></param>
        /// <returns>The <see cref="Response{User, List{ValidationFailure}}"/></returns>
        public Response<User, List<ValidationFailure>> Execute(
            Response<User, List<ValidationFailure>> response,
            UserRequest request)
        {
            // Solo encriptar si se proporcionó una nueva contraseña
            if (!string.IsNullOrEmpty(request.Password) && response.Data != null)
            {
                response.Data.Password = BC.BCrypt.HashPassword(request.Password);
            }

            return response;
        }
    }
}
