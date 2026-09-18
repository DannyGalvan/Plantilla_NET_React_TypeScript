using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Project.Server.Security.Authorization;

namespace Project.Server.Configs.Extensions
{
    public static class AuthorizationConfiguration
    {
        public static IServiceCollection AddOperationAuthorization(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, OperationAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Fail-closed RBAC: every action must be explicitly marked
            // [RequireOperation] or [AllowAnonymous], otherwise the app refuses
            // to start (closes B8). The convention walks the controller model
            // after ASP.NET has built it but before the first request runs.
            services.AddSingleton<IApplicationModelProvider, RequireOperationConventionProvider>();

            return services;
        }
    }

    /// <summary>
    /// Wraps <see cref="RequireOperationConvention"/> in the provider API.
    /// Public so unit tests can exercise the fail-closed check directly.
    /// </summary>
    public sealed class RequireOperationConventionProvider : IApplicationModelProvider
    {
        private readonly RequireOperationConvention _convention = new();

        public int Order => -1000;

        public void OnProvidersExecuting(ApplicationModelProviderContext context) { }

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            foreach (var controller in context.Result.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    _convention.Apply(action);
                }
            }

            // Walk controllers and throw if any action is exposed without
            // [RequireOperation] or [AllowAnonymous].
            foreach (var controller in context.Result.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    if (HasRequireOperation(action)) continue;
                    if (HasAllowAnonymous(action)) continue;
                    if (IsExempt(action)) continue;

                    throw new InvalidOperationException(
                        $"Action '{controller.ControllerName}.{action.ActionName}' exposes a public " +
                        "endpoint but is not decorated with [RequireOperation] or [AllowAnonymous]. " +
                        "Add [RequireOperation] to gate it by OperationKey, or [AllowAnonymous] if " +
                        "the endpoint must be reachable without authentication.");
                }
            }
        }

        private static bool HasRequireOperation(ActionModel action)
            => action.Attributes.OfType<RequireOperationAttribute>().Any();

        private static bool HasAllowAnonymous(ActionModel action)
            => action.Attributes.OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any();

        /// <summary>
        /// Some actions are framework-generated (model binding error fallback,
        /// 404 fallback) and should not be flagged.
        /// </summary>
        private static bool IsExempt(ActionModel action)
            => action.ActionMethod is null; // synthesized by ASP.NET, not user code
    }

    public sealed class RequireOperationConvention : IActionModelConvention
    {
        public void Apply(ActionModel action) { }
    }
}