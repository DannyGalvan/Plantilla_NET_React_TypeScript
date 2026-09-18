using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Project.Server.Configs.Extensions
{
    /// <summary>
    /// Rate limiting. Closes OWASP A07 (Authentication brute force).
    ///
    /// <list type="bullet">
    ///   <item><c>global</c> — fixed window, 60 req / min per IP.</item>
    ///   <item><c>auth</c> — fixed window, 5 attempts / 15 min per IP, applied to
    ///         the login / recovery / token-validation endpoints.</item>
    /// </list>
    /// </summary>
    public static class RateLimiterConfiguration
    {
        public const string GlobalPolicy = "global";
        public const string AuthPolicy = "auth";

        public static IServiceCollection AddAppRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        });
                });

                options.AddPolicy(AuthPolicy, ctx =>
                {
                    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}", _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(15),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        });
                });
            });

            return services;
        }
    }
}