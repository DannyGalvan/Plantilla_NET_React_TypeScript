using Microsoft.Extensions.DependencyInjection;

namespace Project.Application.Jobs;

public enum JobScope
{
    /// <summary>Runs once per active tenant, inside the tenant (its database, settings and time zone).</summary>
    PerTenant,

    /// <summary>Runs once for the whole platform.</summary>
    Platform,
}

/// <summary>
/// Recurring background work (payroll closing, reminders, cleanups, report generation). Resolved from a scope
/// bound to the tenant, so constructor dependencies (repositories, settings, notifications) already see the tenant.
/// The Jobs module schedules it with a distributed lease: one instance runs it at a time.
/// </summary>
public interface IRecurringJob
{
    Task ExecuteAsync(CancellationToken ct);
}

/// <summary>Metadata of a recurring job. <see cref="Cron"/> uses five fields (minute hour day month weekday).</summary>
public sealed record RecurringJobDefinition(string Name, string Cron, JobScope Scope, Type JobType, string? Description);

public static class RecurringJobExtensions
{
    /// <summary>
    /// Declares a job: <c>services.AddRecurringJob&lt;ClosePayrollJob&gt;("payroll.close", "0 2 * * *")</c>
    /// (every day at 02:00 in the time zone of each tenant). Tenants may pause it or change the schedule.
    /// Nothing runs when the Jobs module is not loaded.
    /// </summary>
    public static IServiceCollection AddRecurringJob<TJob>(
        this IServiceCollection services,
        string name,
        string cron,
        JobScope scope = JobScope.PerTenant,
        string? description = null)
        where TJob : class, IRecurringJob
    {
        services.AddScoped<TJob>();
        services.AddSingleton(new RecurringJobDefinition(name, cron, scope, typeof(TJob), description));
        return services;
    }
}
