using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Jobs;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Jobs.Application;
using Project.Modules.Jobs.Contracts;
using Project.Modules.Jobs.Domain;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Jobs.Infrastructure;

public sealed class JobsOptions
{
    public const string SectionName = "Jobs";

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>Longest run of a job; also the lease, after which another instance may take it over.</summary>
    public int LeaseMinutes { get; set; } = 30;
}

public sealed class JobsDbContext(DbContextOptions<JobsDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => "jobs";

    public DbSet<JobState> States => Set<JobState>();

    public DbSet<JobRun> Runs => Set<JobRun>();
}

internal sealed class JobStateConfiguration : IEntityTypeConfiguration<JobState>
{
    public void Configure(EntityTypeBuilder<JobState> builder)
    {
        builder.ToTable("States");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(128).IsRequired();
        builder.Property(s => s.Cron).HasMaxLength(64);
        builder.Property(s => s.LastStatus).HasMaxLength(16);
        builder.Property(s => s.LastError).HasMaxLength(2000);
        builder.HasIndex(s => new { s.TenantId, s.Name }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.NextRunAt });
    }
}

internal sealed class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("Runs");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(128).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(16).IsRequired();
        builder.Property(r => r.Error).HasMaxLength(2000);
        builder.HasIndex(r => new { r.TenantId, r.Name, r.StartedAt });
    }
}

/// <summary>Runs the due jobs of the current scope (one tenant, or the platform).</summary>
public sealed class DueJobsExecutor(
    JobsDbContext db,
    IEnumerable<RecurringJobDefinition> definitions,
    JobSchedule schedule,
    IServiceProvider services,
    IOptions<JobsOptions> options,
    TimeProvider clock,
    ILogger<DueJobsExecutor> logger)
{
    public async Task RunDueAsync(JobScope scope, CancellationToken ct)
    {
        var jobs = definitions.Where(d => d.Scope == scope).ToList();
        if (jobs.Count == 0)
        {
            return;
        }

        await EnsureStatesAsync(jobs, ct);
        var now = clock.GetUtcNow();
        var names = jobs.Select(j => j.Name).ToList();
        var due = await db.States.AsNoTracking()
            .Where(s => names.Contains(s.Name) && s.Enabled && s.NextRunAt <= now && (s.LockedUntil == null || s.LockedUntil < now))
            .ToListAsync(ct);

        foreach (var state in due)
        {
            var lease = TimeSpan.FromMinutes(Math.Max(1, options.Value.LeaseMinutes));
            var lockId = Guid.NewGuid();
            var claimed = await db.States
                .Where(s => s.Id == state.Id && s.NextRunAt <= now && (s.LockedUntil == null || s.LockedUntil < now))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockId, lockId).SetProperty(x => x.LockedUntil, now.Add(lease)), ct);
            if (claimed == 1)
            {
                await RunAsync(jobs.First(j => j.Name == state.Name), state.Id, lease, ct);
            }
        }
    }

    private async Task RunAsync(RecurringJobDefinition definition, Guid stateId, TimeSpan lease, CancellationToken ct)
    {
        var started = clock.GetUtcNow();
        string status = JobStatuses.Succeeded;
        string? error = null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(lease);

        try
        {
            var job = (IRecurringJob)services.GetRequiredService(definition.JobType);
            await job.ExecuteAsync(timeout.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            status = JobStatuses.Failed;
            error = ex.Message;
            logger.LogError(ex, "Recurring job {Job} failed", definition.Name);
        }

        db.ChangeTracker.Clear();
        var state = await db.States.FirstAsync(s => s.Id == stateId, ct);
        var finished = clock.GetUtcNow();
        state.Finish(status, error, started, finished, await schedule.NextAsync(state.Cron ?? definition.Cron, finished, ct));
        db.Runs.Add(JobRun.Create(definition.Name, started, finished, status, error));
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureStatesAsync(IReadOnlyList<RecurringJobDefinition> jobs, CancellationToken ct)
    {
        var names = jobs.Select(j => j.Name).ToList();
        var existing = await db.States.Where(s => names.Contains(s.Name)).Select(s => s.Name).ToListAsync(ct);
        var missing = jobs.Where(j => !existing.Contains(j.Name)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        foreach (var job in missing)
        {
            db.States.Add(JobState.Create(job.Name, await schedule.NextAsync(job.Cron, clock.GetUtcNow(), ct)));
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another instance created them first (unique index on tenant and name).
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }
}

/// <summary>Ticks every few seconds and runs the due jobs of every tenant, then the platform jobs.</summary>
public sealed class JobScheduler(
    IServiceScopeFactory scopes,
    ITenantJobRunner tenants,
    IOptions<JobsOptions> options,
    TimeProvider clock,
    ILogger<JobScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.Value.PollingIntervalSeconds)), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await tenants.ForEachTenantAsync((sp, _, ct) => sp.GetRequiredService<DueJobsExecutor>().RunDueAsync(JobScope.PerTenant, ct), stoppingToken);
                await using var platform = scopes.CreateAsyncScope();
                await platform.ServiceProvider.GetRequiredService<DueJobsExecutor>().RunDueAsync(JobScope.Platform, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "The job scheduler tick failed; retrying on the next tick");
            }
        }
    }
}

public sealed record UpdateJobRequest(bool Enabled, string? Cron);

/// <summary>
/// Scheduler of the recurring jobs declared by every module (<c>AddRecurringJob</c>): per tenant, in its time zone,
/// with a distributed lease, history and manual runs.
/// </summary>
public sealed class JobsModule : IModule
{
    public string Name => "Jobs";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<JobsDbContext>(Name, DataScope);
        services.AddRepositoriesFor<JobsDbContext>();
        services.AddModuleConventions(typeof(JobsPermissionProvider).Assembly);
        services.AddOptions<JobsOptions>().Bind(configuration.GetSection(JobsOptions.SectionName));
        services.AddScoped<JobSchedule>();
        services.AddScoped<DueJobsExecutor>();
        services.AddHostedService<JobScheduler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var jobs = api.MapGroup("jobs").WithTags("jobs").RequirePermission(JobsPermissions.Jobs.Manage);
        jobs.MapGet("/", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListJobsQuery(), ct)).ToHttp())
            .WithName("ListJobs");
        jobs.MapPut("/{name}", async (string name, UpdateJobRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateJobCommand(name, request.Enabled, request.Cron), ct)).ToHttp())
            .WithName("UpdateJob");
        jobs.MapPost("/{name}/run", async (string name, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RunJobNowCommand(name), ct)).ToHttp())
            .WithName("RunJobNow");
        jobs.MapGet("/{name}/runs", async (string name, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListJobRunsQuery(name), ct)).ToHttp())
            .WithName("ListJobRuns");
    }
}
