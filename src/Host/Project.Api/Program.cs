using Project.Application.Messaging;
using Project.Composition;
using Project.ServiceDefaults;
using Project.Web;
using Project.Web.Modules;

// Container health probe without curl: `dotnet Project.Api.dll --healthcheck`.
if (args.Contains("--healthcheck"))
{
    var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';')[0] ?? "8080";
    using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    try
    {
        return (await probe.GetAsync(new Uri($"http://localhost:{port}/health/live"))).IsSuccessStatusCode ? 0 : 1;
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        return 1;
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddPlatform();
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>), typeof(AuthorizationBehavior<,>)];
});

builder.AddModules(AppModules.All());

var app = builder.Build();

app.UsePlatform();
app.MapModules();
app.MapDefaultEndpoints();
app.MapFallbackToFile("/index.html").AllowAnonymous();

app.Run();
return 0;

/// <summary>Entry point, public for WebApplicationFactory in the functional tests.</summary>
public partial class Program;
