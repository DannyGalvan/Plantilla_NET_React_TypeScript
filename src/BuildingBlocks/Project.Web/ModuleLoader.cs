using Microsoft.AspNetCore.Routing;

namespace Project.Web;

/// <summary>
/// Builder extensions that wire all modules. A module whose name appears in
/// <c>Modules:Disabled</c> is skipped (its services and endpoints are not loaded).
/// </summary>
public static class ModuleLoader
{
    public static WebApplicationBuilder AddModules(this WebApplicationBuilder builder, params IModule[] modules)
    {
        var disabled = builder.Configuration.GetSection("Modules:Disabled").Get<string[]>() ?? [];

        foreach (var module in modules)
        {
            if (disabled.Contains(module.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            module.AddServices(builder.Services, builder.Configuration);
        }

        builder.Services.AddSingleton<IEnumerable<IModule>>(modules);
        return builder;
    }

    public static IEndpointRouteBuilder MapModules(this IEndpointRouteBuilder endpoints)
    {
        var sp = endpoints.ServiceProvider;
        var modules = sp.GetRequiredService<IEnumerable<IModule>>();

        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}