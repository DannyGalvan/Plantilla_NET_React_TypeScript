using Project.Application.Messaging;
using Project.Modules.Catalog.Infrastructure;
using Project.Modules.Identity.Infrastructure;
using Project.Modules.Organization.Infrastructure;
using Project.Modules.Tenancy.Infrastructure;
using Project.ServiceDefaults;
using Project.Web;
using Project.Web.Modules;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddPlatform();
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>), typeof(AuthorizationBehavior<,>)];
});

// One line per module. Removing OrganizationModule turns the app into a single-company application.
builder.AddModules(
    new TenancyModule(),
    new IdentityModule(),
    new OrganizationModule(),
    new CatalogModule());

var app = builder.Build();

app.UsePlatform();
app.MapModules();
app.MapDefaultEndpoints();
app.MapFallbackToFile("/index.html").AllowAnonymous();

app.Run();

/// <summary>Entry point, public for WebApplicationFactory in the functional tests.</summary>
public partial class Program;
