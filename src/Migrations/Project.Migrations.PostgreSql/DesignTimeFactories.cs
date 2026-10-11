using Microsoft.EntityFrameworkCore.Design;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Persistence.Providers;
using Project.Modules.Catalog.Infrastructure;
using Project.Modules.Identity.Infrastructure.Persistence;
using Project.Modules.Organization.Infrastructure.Persistence;
using Project.Modules.Tenancy.Infrastructure.Persistence;

namespace Project.Migrations.PostgreSql;

// Design-time factories used by dotnet ef. One per module DbContext.

public sealed class TenancyDbContextFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    public TenancyDbContext CreateDbContext(string[] args) => DesignTime.Create<TenancyDbContext>(new PostgreSqlProvider(), "Tenancy");
}

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args) => DesignTime.Create<IdentityDbContext>(new PostgreSqlProvider(), "Identity");
}

public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args) => DesignTime.Create<OrganizationDbContext>(new PostgreSqlProvider(), "Organization");
}

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) => DesignTime.Create<CatalogDbContext>(new PostgreSqlProvider(), "Catalog");
}
