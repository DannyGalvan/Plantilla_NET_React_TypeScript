using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Project.Application.Tenancy;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Readiness: every platform DbContext (and the shared tenant database) can open a connection. Dedicated tenant
/// databases are not probed here so a single slow tenant does not take the instance out of rotation.
/// </summary>
public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var registrations = scope.ServiceProvider.GetServices<ModuleDbContextRegistration>()
            .OrderBy(r => r.Scope == DataScope.Platform ? 0 : 1)
            .ToList();

        var checkedConnections = new HashSet<string>();
        foreach (var registration in registrations)
        {
            var db = (DbContext)scope.ServiceProvider.GetRequiredService(registration.ContextType);
            var connection = db.Database.GetConnectionString() ?? registration.Module;
            if (!checkedConnections.Add(connection))
            {
                continue;
            }

            if (!await db.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy($"Database of module {registration.Module} is not reachable.");
            }
        }

        return HealthCheckResult.Healthy();
    }
}
