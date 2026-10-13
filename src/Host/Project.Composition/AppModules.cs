using Project.Modules.Catalog.Infrastructure;
using Project.Modules.Identity.Infrastructure;
using Project.Modules.Organization.Infrastructure;
using Project.Modules.Tenancy.Infrastructure;
using Project.Modules.Files.Infrastructure;
using Project.Modules.Notifications.Infrastructure;
using Project.Modules.Audit.Infrastructure;
using Project.Modules.Jobs.Infrastructure;
using Project.Web.Modules;

namespace Project.Composition;

/// <summary>
/// The modules of this application. Adding a module is one line here (plus its project reference); the API and the
/// migration service both load this list. Removing OrganizationModule turns the app into a single-company application.
/// A module can also be switched off without code with <c>Modules:Disabled</c> in configuration.
/// </summary>
public static class AppModules
{
    public static IModule[] All() =>
    [
        new TenancyModule(),
        new IdentityModule(),
        new OrganizationModule(),
        new CatalogModule(),
        new FilesModule(),
        new NotificationsModule(),
        new AuditModule(),
        new JobsModule(),
    ];
}
