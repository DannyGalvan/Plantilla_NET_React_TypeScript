using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Project.Server.Configs.Extensions
{
    /// <summary>
    /// Explicit CORS allowlist. Closes OWASP A05.
    /// Origins come from <c>AppSettings:Cors:AllowedOrigins</c>; a missing or empty
    /// array means no cross-origin requests are accepted (safe default).
    /// </summary>
    public static class CorsConfiguration
    {
        public const string PolicyName = "AppCors";

        public static IServiceCollection AddAppCors(this IServiceCollection services, IEnumerable<string> allowedOrigins)
        {
            var origins = allowedOrigins?.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray() ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, builder =>
                {
                    if (origins.Length > 0)
                    {
                        builder.WithOrigins(origins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                    else
                    {
                        builder.SetIsOriginAllowed(_ => false)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                });
            });

            return services;
        }
    }
}