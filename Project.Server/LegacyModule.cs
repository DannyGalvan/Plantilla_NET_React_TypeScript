using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Project.Server.Configs.Extensions;
using Project.Server.Configs.Models;
using Project.Server.Infrastructure.Extensions;
using Project.Web;

namespace Project.Server;

/// <summary>
/// Wraps the legacy single-project web app as a temporary module.
/// It registers the existing controllers, DbContext, JWT, rate limiter, CORS
/// and Swagger configuration. Will be removed in Fase 4 when the legacy code
/// is decomposed into per-module projects.
/// </summary>
public sealed class LegacyModule : IModule
{
    public string Name => "Legacy";

    public DataScope DataScope => DataScope.Platform;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>() ?? new AppSettings();

        services.AddSingleton(appSettings);
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        services.AddEndpointsApiExplorer();
        services.AddMapsterSettings();
        services.AddJwtConfiguration(appSettings);
        services.AddOperationAuthorization();
        services.AddSwaggerConfiguration();
        services.AddContextGroup(configuration);
        services.AddValidationsGroup();
        services.AddServiceGroup();
        services.AddControllersConfiguration();
        services.AddAppCors(appSettings.CorsAllowedOrigins);
        services.AddAppRateLimiter();
        services.AddProblemDetails();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
    }
}