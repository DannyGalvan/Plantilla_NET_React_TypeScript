using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Organization;
using Project.Application.Security;
using Project.Application.Tenancy;
using Project.Infrastructure.Tenancy;

namespace Project.Infrastructure.Persistence;

public enum SeederScope
{
    /// <summary>Runs once against the platform database.</summary>
    Platform,

    /// <summary>Runs once per active tenant with the tenant context set.</summary>
    PerTenant,
}

/// <summary>Idempotent seed step declared by a module.</summary>
public interface IDataSeeder
{
    int Order { get; }

    SeederScope Scope { get; }

    Task SeedAsync(IServiceProvider services, CancellationToken ct);
}

/// <summary>
/// Applies migrations, synchronizes permissions and runs seeders, in this order:
/// platform contexts (plus tenant contexts that live in the shared database), platform seeders,
/// permission synchronization, dedicated tenant databases, and per-tenant seeders.
/// Every database is migrated under a distributed lock, so concurrent runs are safe.
/// </summary>
public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<TenancyOptions> tenancy,
    ILogger<DatabaseInitializer> logger)
{
    private const string LockName = "project-migrations";
    private const int MaxCreateAttempts = 5;

    public async Task InitializeAsync(string? onlyTenantCode, CancellationToken ct)
    {
        await InitializePlatformAsync(ct);
        await InitializeTenantsAsync(onlyTenantCode, ct);
    }

    /// <summary>Migrates the platform database (with the shared tenant schemas), runs platform seeders and syncs permissions.</summary>
    public async Task InitializePlatformAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var registrations = scope.ServiceProvider.GetServices<ModuleDbContextRegistration>().ToList();

        // Tenant contexts are also created in the platform database: it hosts every shared tenant
        // (and the implicit default tenant), whatever the mode.
        foreach (var registration in registrations.OrderBy(r => r.Scope))
        {
            await MigrateAsync(scope.ServiceProvider, registration, "platform", ct);
        }

        await scope.ServiceProvider.GetRequiredService<IOrganizationContext>().InitializeAsync(ct);
        await RunSeedersAsync(scope.ServiceProvider, SeederScope.Platform, ct);

        var synchronizer = scope.ServiceProvider.GetService<IPermissionSynchronizer>();
        if (synchronizer is not null)
        {
            await synchronizer.SynchronizeAsync(ct);
            logger.LogInformation("Permissions synchronized");
        }
    }

    /// <summary>Migrates dedicated tenant databases and runs per-tenant seeders for every active tenant.</summary>
    public async Task InitializeTenantsAsync(string? onlyTenantCode, CancellationToken ct)
    {
        await using var listing = scopeFactory.CreateAsyncScope();
        var tenants = tenancy.Value.Mode == TenancyMode.None
            ? [TenantContext.DefaultTenant]
            : await listing.ServiceProvider.GetRequiredService<ITenantStore>().ListActiveAsync(ct);

        var failures = 0;
        foreach (var tenant in tenants.Where(t => onlyTenantCode is null || t.Code == onlyTenantCode))
        {
            try
            {
                await InitializeTenantAsync(tenant, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures++;
                logger.LogError(ex, "Initialization failed for tenant {Tenant}", tenant.Code);
            }
        }

        if (failures > 0)
        {
            throw new InvalidOperationException($"{failures} tenant(s) failed to initialize. See the log for details.");
        }
    }

    /// <summary>Migrates the dedicated database of a tenant (if any) and runs the per-tenant seeders.</summary>
    public async Task InitializeTenantAsync(TenantInfo tenant, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantContext>();
        if (tenancy.Value.Mode != TenancyMode.None)
        {
            context.Set(tenant);
        }

        await scope.ServiceProvider.GetRequiredService<ITenantConnectionResolver>().PrepareAsync(tenant, ct);

        if (context.UsesDedicatedDatabase)
        {
            foreach (var registration in scope.ServiceProvider.GetServices<ModuleDbContextRegistration>().Where(r => r.Scope == DataScope.Tenant))
            {
                await MigrateAsync(scope.ServiceProvider, registration, tenant.Code, ct);
            }
        }

        // The organization scope reads tenant tables, so it is loaded once the database is migrated.
        await scope.ServiceProvider.GetRequiredService<IOrganizationContext>().InitializeAsync(ct);
        await RunSeedersAsync(scope.ServiceProvider, SeederScope.PerTenant, ct);
        logger.LogInformation("Tenant {Tenant} initialized", tenant.Code);
    }

    private async Task MigrateAsync(IServiceProvider services, ModuleDbContextRegistration registration, string target, CancellationToken ct)
    {
        var context = (ModuleDbContext)services.GetRequiredService(registration.ContextType);
        var database = context.Database;

        var creator = database.GetService<IRelationalDatabaseCreator>();
        for (var attempt = 1; !await creator.ExistsAsync(ct); attempt++)
        {
            logger.LogInformation("Creating database for {Module} ({Target})", registration.Module, target);
            try
            {
                await creator.CreateAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < MaxCreateAttempts)
            {
                // Another instance may have created it in the meantime, or the engine rejected a concurrent
                // CREATE DATABASE (PostgreSQL: "source database template1 is being accessed by other users").
                logger.LogWarning(ex, "Creating database for {Module} ({Target}) failed, attempt {Attempt}", registration.Module, target, attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), ct);
            }
        }

        await using var connection = context.Provider.CreateConnection(database.GetConnectionString()!);
        await using (await context.Provider.AcquireLockAsync(connection, LockName, ct))
        {
            await database.MigrateAsync(ct);
            var applied = (await database.GetAppliedMigrationsAsync(ct)).ToList();
            logger.LogInformation("{Module} ({Target}) is at migration {Migration}", registration.Module, target, applied.LastOrDefault() ?? "none");
        }
    }

    private static async Task RunSeedersAsync(IServiceProvider services, SeederScope scope, CancellationToken ct)
    {
        foreach (var seeder in services.GetServices<IDataSeeder>().Where(s => s.Scope == scope).OrderBy(s => s.Order))
        {
            await seeder.SeedAsync(services, ct);
        }
    }
}

/// <summary>Development convenience: runs the initializer when the API starts if <c>Database:MigrateOnStartup</c> is true.</summary>
public sealed class MigrateOnStartupService(
    DatabaseInitializer initializer,
    IOptions<DatabaseOptions> options,
    IHostEnvironment environment) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.MigrateOnStartup)
        {
            return Task.CompletedTask;
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Database:MigrateOnStartup is not allowed in Production. Run Project.MigrationService as a deployment step.");
        }

        return initializer.InitializeAsync(null, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
