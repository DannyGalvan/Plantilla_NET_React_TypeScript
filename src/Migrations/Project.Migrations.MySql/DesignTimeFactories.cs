using Microsoft.EntityFrameworkCore.Design;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Persistence.Providers;
using Project.Modules.Sequences.Infrastructure;
using Project.Modules.Jobs.Infrastructure;
using Project.Modules.Audit.Infrastructure;
using Project.Modules.Notifications.Infrastructure;
using Project.Modules.Files.Infrastructure;
using Project.Modules.Catalog.Infrastructure;
using Project.Modules.Identity.Infrastructure.Persistence;
using Project.Modules.Organization.Infrastructure.Persistence;
using Project.Modules.Tenancy.Infrastructure.Persistence;

namespace Project.Migrations.MySql;

// Design-time factories used by dotnet ef. One per module DbContext.

public sealed class TenancyDbContextFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    public TenancyDbContext CreateDbContext(string[] args) => DesignTime.Create<TenancyDbContext>(new MySqlProvider(), "Tenancy");
}

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args) => DesignTime.Create<IdentityDbContext>(new MySqlProvider(), "Identity");
}

public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args) => DesignTime.Create<OrganizationDbContext>(new MySqlProvider(), "Organization");
}

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) => DesignTime.Create<CatalogDbContext>(new MySqlProvider(), "Catalog");
}

public sealed class FilesDbContextFactory : IDesignTimeDbContextFactory<FilesDbContext>
{
    public FilesDbContext CreateDbContext(string[] args) => DesignTime.Create<FilesDbContext>(new MySqlProvider(), "Files");
}

public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args) => DesignTime.Create<NotificationsDbContext>(new MySqlProvider(), "Notifications");
}

public sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args) => DesignTime.Create<AuditDbContext>(new MySqlProvider(), "Audit");
}

public sealed class JobsDbContextFactory : IDesignTimeDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext(string[] args) => DesignTime.Create<JobsDbContext>(new MySqlProvider(), "Jobs");
}

public sealed class SequencesDbContextFactory : IDesignTimeDbContextFactory<SequencesDbContext>
{
    public SequencesDbContext CreateDbContext(string[] args) => DesignTime.Create<SequencesDbContext>(new MySqlProvider(), "Sequences");
}
