using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Project.Server.Configs.Extensions;
using Project.Server.Configs.Models;
using Project.Server.Infrastructure.Extensions;

namespace Project.Server
{
    public abstract class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string environment = builder.Environment.EnvironmentName;

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            IConfigurationSection appSettingsSection = configuration.GetSection("AppSettings");
            AppSettings appSettingsConfig = appSettingsSection.Get<AppSettings>()!;

            builder.Services.Configure<AppSettings>(appSettingsSection);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddMapsterSettings();
            builder.Services.AddJwtConfiguration(appSettingsConfig);
            builder.Services.AddOperationAuthorization();
            builder.Services.AddSwaggerConfiguration();
            builder.Services.AddContextGroup(configuration);
            builder.Services.AddValidationsGroup();
            builder.Services.AddServiceGroup();
            builder.Services.AddControllersConfiguration();
            builder.Services.AddAppCors(appSettingsConfig.CorsAllowedOrigins);
            builder.Services.AddAppRateLimiter();
            builder.Services.AddProblemDetails();

            var app = builder.Build();

            app.ApplyMigrations(configuration);

            // UseExceptionHandler with ProblemDetails (RFC 7807). Closes B11
            // by replacing the framework's HTML 500 page with a JSON envelope
            // that contains only the trace id.
            app.UseExceptionHandler(handler =>
            {
                handler.Run(async context =>
                {
                    var feature = context.Features.Get<IExceptionHandlerFeature>();
                    var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
                    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("UnhandledException");
                    if (feature?.Error is { } ex)
                    {
                        logger.LogError(ex, "Unhandled exception (traceId={traceId})", traceId);
                    }
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        type = "about:blank",
                        title = "An unexpected error occurred.",
                        status = StatusCodes.Status500InternalServerError,
                        detail = $"traceId={traceId}",
                        traceId,
                    });
                });
            });

            app.UseSecurityHeaders();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseCors(CorsConfiguration.PolicyName);

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}