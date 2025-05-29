using Project.Server.Entities.Response;

namespace Project.Server.Configs.Extensions
{
    using FluentValidation.Results;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    /// <summary>
    /// Defines the <see cref="ControllerConfiguration" />
    /// </summary>
    public static class ControllerConfiguration
    {
        /// <summary>
        /// The AddControllersConfiguration
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/></param>
        public static void AddControllersConfiguration(this IServiceCollection services)
        {
            //Add FluentValidation to response errors of the controllers
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = false;
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        List<ValidationFailure> failures = [];

                        context.ModelState.ToList().ForEach(state =>
                        {
                            ModelStateEntry? value = state.Value;

                            IEnumerable<ValidationFailure>? errors = value?.Errors.ToList().Select(e => new ValidationFailure(state.Key, e.ErrorMessage, value.AttemptedValue));

                            if (errors != null)
                                failures.AddRange(errors);
                        });

                        var result = new Response<List<ValidationFailure>>()
                        {
                            Success = false,
                            Message = "La petición no es valida, contiene errores, porfavor revise",
                            Data = failures
                        };

                        return new BadRequestObjectResult(result);
                    };
                });
        }
    }
}
