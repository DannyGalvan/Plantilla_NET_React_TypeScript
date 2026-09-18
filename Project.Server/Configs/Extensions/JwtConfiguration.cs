using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Project.Server.Configs.Models;
using Project.Server.Entities.Response;

namespace Project.Server.Configs.Extensions
{
    /// <summary>
    /// Hardened JWT configuration. Closes OWASP A02 (Cryptographic Failures) and
    /// A07 (token validation):
    /// <list type="bullet">
    ///   <item>ValidateIssuer / ValidateAudience both enabled and bound from config.</item>
    ///   <item>HTTPS metadata required outside Development.</item>
    ///   <item>ValidAlgorithms pinned to <c>HS256</c> (no algorithm confusion).</item>
    ///   <item>ClockSkew tightened to 30s.</item>
    ///   <item>UTF-8 key, with a startup-time guard rejecting keys &lt; 32 bytes.</item>
    ///   <item>Default token expiration capped at 1h when config is missing.</item>
    /// </list>
    /// </summary>
    public static class JwtConfiguration
    {
        public static IServiceCollection AddJwtConfiguration(this IServiceCollection services,
            AppSettings appSettingsConfig)
        {
            if (string.IsNullOrWhiteSpace(appSettingsConfig.Secret))
            {
                throw new InvalidOperationException(
                    "AppSettings:Secret is required. Configure via user-secrets or env var " +
                    "AppSettings__Secret (the latter is consumed via the [AppSettings] binder).");
            }

            byte[] key = Encoding.UTF8.GetBytes(appSettingsConfig.Secret);
            if (key.Length < 32)
            {
                throw new InvalidOperationException(
                    $"AppSettings:Secret must be at least 32 bytes (got {key.Length}). " +
                    "Use a randomly generated 256-bit key.");
            }

            if (appSettingsConfig.TokenExpirationHrs <= 0)
            {
                appSettingsConfig.TokenExpirationHrs = 1;
            }

            var issuer = string.IsNullOrWhiteSpace(appSettingsConfig.Issuer) ? "Plantilla" : appSettingsConfig.Issuer;
            var audience = string.IsNullOrWhiteSpace(appSettingsConfig.Audience) ? "Plantilla.Client" : appSettingsConfig.Audience;

            services.AddAuthentication(d =>
                {
                    d.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    d.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(d =>
                {
                    d.RequireHttpsMetadata = true;
                    d.SaveToken = true;
                    d.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                        ClockSkew = TimeSpan.FromSeconds(30),
                    };
                    d.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                StringValues expiredHeader = new("true");
                                context.Response.Headers.Append("Token-Expired", expiredHeader);
                            }
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";

                            var result = JsonSerializer.Serialize(
                                new Response<string>
                                {
                                    Success = false,
                                    Message = "Unauthorized",
                                    Data = null
                                });

                            return context.Response.WriteAsync(result);
                        }
                    };
                });

            return services;
        }
    }
}