using FluentValidation.Results;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Interceptors.Interfaces;
using BC = BCrypt.Net;

namespace Project.Server.Interceptors.UserInterceptors
{
    /// <summary>
    /// Defines the <see cref="UserBeforeCreateInterceptor" />
    /// </summary>
    public class UserBeforeCreateInterceptor : IEntityBeforeCreateInterceptor<User, UserRequest>
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
            // Verificar que el password fue proporcionado
            if (string.IsNullOrEmpty(request.Password))
            {
                response.Success = false;
                response.Errors = [new ValidationFailure("Password", "La contraseña es requerida al crear un usuario.")];
                return response;
            }

            // Encriptar la contraseña antes de guardar
            if (response.Data != null)
            {
                response.Data.Password = BC.BCrypt.HashPassword(request.Password);
            }

            return response;
        }
    }
}
