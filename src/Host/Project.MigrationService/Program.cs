using Microsoft.Extensions.Options;
using Project.Application.Tenancy;
using Project.Infrastructure;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Tenancy;
using Project.Modules.Catalog.Infrastructure;
using Project.Modules.Identity.Infrastructure;
using Project.Modules.Organization.Infrastructure;
using Project.Modules.Tenancy.Application.Tenants;
using Project.Modules.Tenancy.Infrastructure;
using Project.ServiceDefaults;
using Project.Web.Modules;
using Project.Web.Security;

// Usage:
//   dotnet run --project src/Host/Project.MigrationService                      migrate everything
//   dotnet run --project src/Host/Project.MigrationService -- migrate --tenant acme
//   dotnet run --project src/Host/Project.MigrationService -- provision-tenant acme
// Exit code 0 on success, 1 on failure (so pipelines stop the deployment).

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.AddServiceDefaults();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<JwtOptions>().Bind(builder.Configuration.GetSection(JwtOptions.SectionName));

// Same module list as the API (the migration service must know every DbContext and seeder).
builder.AddModules(
    new TenancyModule(),
    new IdentityModule(),
    new OrganizationModule(),
    new CatalogModule());

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationService");
var initializer = host.Services.GetRequiredService<DatabaseInitializer>();
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(30));

try
{
    var command = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "migrate";
    var tenantCode = Option("--tenant") ?? (command == "provision-tenant" ? args.Skip(1).FirstOrDefault(a => !a.StartsWith('-')) : null);

    switch (command)
    {
        case "migrate":
            await initializer.InitializeAsync(tenantCode, cancellation.Token);
            break;

        case "provision-tenant" when !string.IsNullOrWhiteSpace(tenantCode):
            await initializer.InitializePlatformAsync(cancellation.Token);
            await using (var scope = host.Services.CreateAsyncScope())
            {
                var tenant = await scope.ServiceProvider.GetRequiredService<ITenantStore>().FindByCodeAsync(tenantCode, cancellation.Token)
                    ?? throw new InvalidOperationException($"Tenant '{tenantCode}' does not exist. Create it first from the API.");
                var provisioned = await scope.ServiceProvider.GetRequiredService<ITenantProvisioner>().ProvisionAsync(tenant.Id, cancellation.Token);
                if (provisioned.IsFailure)
                {
                    throw new InvalidOperationException(provisioned.Error!.Description);
                }
            }

            break;

        default:
            logger.LogError("Unknown command '{Command}'. Use: migrate [--tenant code] | provision-tenant code", command);
            return 1;
    }

    logger.LogInformation("Database initialization finished ({Mode} mode)", host.Services.GetRequiredService<IOptions<TenancyOptions>>().Value.Mode);
    return 0;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Database initialization failed");
    return 1;
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
