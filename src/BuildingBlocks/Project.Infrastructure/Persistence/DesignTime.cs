using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Project.Infrastructure.Persistence.Providers;
using Project.Infrastructure.Services;
using Project.Infrastructure.Tenancy;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Builds module DbContexts for <c>dotnet ef</c> (migrations projects). No database connection is opened:
/// the connection string is a placeholder unless <c>EF_CONNECTION</c> is set.
/// </summary>
public static class DesignTime
{
    public static TContext Create<TContext>(IDatabaseProvider provider, string module)
        where TContext : ModuleDbContext
    {
        var connection = Environment.GetEnvironmentVariable("EF_CONNECTION") ?? provider.Name switch
        {
            "SqlServer" => "Server=localhost;Database=project_design;Trusted_Connection=True;TrustServerCertificate=True",
            "MySql" => "Server=localhost;Database=project_design;User=root;Password=design",
            _ => "Host=localhost;Database=project_design;Username=postgres;Password=design",
        };

        var builder = new DbContextOptionsBuilder<TContext>();
        provider.Configure(builder, connection, new ProviderSettings(module.ToLowerInvariant(), $"Project.Migrations.{provider.Name}", 30, 0));

        var dependencies = new ModuleDbContextDependencies(
            new TenantContext(Options.Create(new TenancyOptions())),
            new NoOpOrganizationContext(),
            new DatabaseProviderRegistry([provider]));

        return (TContext)Activator.CreateInstance(typeof(TContext), builder.Options, dependencies)!;
    }
}
