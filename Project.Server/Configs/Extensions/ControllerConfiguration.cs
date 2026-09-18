using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Entities.Response;

namespace Project.Server.Configs.Extensions
{
    /// <summary>
    /// Controller-level wiring. Closes OWASP A05 — model-validation responses no
    /// longer echo the request value (which previously surfaced submitted
    /// passwords back to the caller).
    /// </summary>
    public static class ControllerConfiguration
    {
        public static IServiceCollection AddControllersConfiguration(this IServiceCollection services)
        {
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = false;
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var failures = new List<ValidationFailure>();

                        foreach (var state in context.ModelState)
                        {
                            if (state.Value is null) continue;
                            foreach (var err in state.Value.Errors)
                            {
                                // Only the field name + error message go out.
                                // AttemptedValue is intentionally dropped to
                                // prevent echoing back sensitive fields such
                                // as passwords.
                                failures.Add(new ValidationFailure(state.Key, err.ErrorMessage));
                            }
                        }

                        var result = new Response<List<ValidationFailure>>
                        {
                            Success = false,
                            Message = "The request is invalid.",
                            Data = failures
                        };

                        return new BadRequestObjectResult(result);
                    };
                });

            return services;
        }
    }
}