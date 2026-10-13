using Cronos;
using Project.Application.Abstractions;
using Project.Application.Jobs;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Application.Settings;
using Project.Modules.Jobs.Contracts;
using Project.Modules.Jobs.Domain;
using Project.SharedKernel;

namespace Project.Modules.Jobs.Application;

/// <summary>Next occurrence of a cron expression in the time zone of the tenant.</summary>
public sealed class JobSchedule(ISettingsProvider settings)
{
    public static bool IsValid(string cron) => CronExpression.TryParse(cron, CronFormat.Standard, out _);

    public async Task<DateTimeOffset> NextAsync(string cron, DateTimeOffset after, CancellationToken ct)
    {
        var zone = await ZoneAsync(ct);
        return CronExpression.Parse(cron, CronFormat.Standard).GetNextOccurrence(after, zone) ?? DateTimeOffset.MaxValue;
    }

    private async Task<TimeZoneInfo> ZoneAsync(CancellationToken ct)
    {
        var localization = await settings.GetAsync<LocalizationSettings>(ct);
        return TimeZoneInfo.TryFindSystemTimeZoneById(localization.TimeZone, out var zone) ? zone : TimeZoneInfo.Utc;
    }
}

public sealed record JobDto(
    string Name,
    string? Description,
    string Scope,
    string DefaultCron,
    string? Cron,
    bool Enabled,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastFinishedAt,
    string? LastStatus,
    string? LastError);

public sealed record ListJobsQuery : IQuery<IReadOnlyList<JobDto>>, IRequirePermission
{
    public string Permission => JobsPermissions.Jobs.Manage;
}

public sealed class ListJobsHandler(IEnumerable<RecurringJobDefinition> definitions, IRepository<JobState, Guid> states)
    : IQueryHandler<ListJobsQuery, IReadOnlyList<JobDto>>
{
    public async ValueTask<Result<IReadOnlyList<JobDto>>> Handle(ListJobsQuery query, CancellationToken cancellationToken)
    {
        var current = await states.ListAsync(new Specification<JobState>(), cancellationToken);
        IReadOnlyList<JobDto> jobs = definitions
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .Select(d =>
            {
                var state = current.FirstOrDefault(s => s.Name == d.Name);
                return new JobDto(
                    d.Name,
                    d.Description,
                    d.Scope.ToString(),
                    d.Cron,
                    state?.Cron,
                    state?.Enabled ?? true,
                    state?.NextRunAt,
                    state?.LastStartedAt,
                    state?.LastFinishedAt,
                    state?.LastStatus,
                    state?.LastError);
            })
            .ToList();
        return Result<IReadOnlyList<JobDto>>.Success(jobs);
    }
}

/// <summary>Pauses or resumes a job, or changes its schedule (null cron restores the default) for the tenant.</summary>
public sealed record UpdateJobCommand(string Name, bool Enabled, string? Cron) : ICommand, IRequirePermission
{
    public string Permission => JobsPermissions.Jobs.Manage;
}

public sealed class UpdateJobHandler(
    IEnumerable<RecurringJobDefinition> definitions,
    IRepository<JobState, Guid> states,
    JobSchedule schedule,
    TimeProvider clock) : ICommandHandler<UpdateJobCommand>
{
    public async ValueTask<Result> Handle(UpdateJobCommand command, CancellationToken cancellationToken)
    {
        var definition = definitions.FirstOrDefault(d => d.Name == command.Name);
        if (definition is null)
        {
            return Error.NotFound("jobs.not_found");
        }

        if (!string.IsNullOrWhiteSpace(command.Cron) && !JobSchedule.IsValid(command.Cron))
        {
            return Error.Validation("cron", "The schedule is not a valid cron expression (minute hour day month weekday).");
        }

        var state = await JobStates.GetOrCreateAsync(states, definition, schedule, clock, cancellationToken);
        var cron = string.IsNullOrWhiteSpace(command.Cron) ? definition.Cron : command.Cron;
        state.Configure(command.Enabled, command.Cron, await schedule.NextAsync(cron, clock.GetUtcNow(), cancellationToken));
        await states.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Queues an immediate run (picked up by the scheduler on its next tick).</summary>
public sealed record RunJobNowCommand(string Name) : ICommand, IRequirePermission
{
    public string Permission => JobsPermissions.Jobs.Manage;
}

public sealed class RunJobNowHandler(
    IEnumerable<RecurringJobDefinition> definitions,
    IRepository<JobState, Guid> states,
    JobSchedule schedule,
    TimeProvider clock) : ICommandHandler<RunJobNowCommand>
{
    public async ValueTask<Result> Handle(RunJobNowCommand command, CancellationToken cancellationToken)
    {
        var definition = definitions.FirstOrDefault(d => d.Name == command.Name);
        if (definition is null)
        {
            return Error.NotFound("jobs.not_found");
        }

        var state = await JobStates.GetOrCreateAsync(states, definition, schedule, clock, cancellationToken);
        state.RunNow(clock.GetUtcNow());
        await states.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record JobRunDto(Guid Id, string Name, DateTimeOffset StartedAt, DateTimeOffset FinishedAt, string Status, string? Error);

public sealed record ListJobRunsQuery(string Name) : IQuery<IReadOnlyList<JobRunDto>>, IRequirePermission
{
    public string Permission => JobsPermissions.Jobs.Manage;
}

public sealed class ListJobRunsHandler(IRepository<JobRun, Guid> runs) : IQueryHandler<ListJobRunsQuery, IReadOnlyList<JobRunDto>>
{
    public async ValueTask<Result<IReadOnlyList<JobRunDto>>> Handle(ListJobRunsQuery query, CancellationToken cancellationToken)
    {
        var items = await runs.ListAsync(
            new Specification<JobRun>().Where(r => r.Name == query.Name).OrderBy(r => r.StartedAt, descending: true).Page(0, 50),
            cancellationToken);
        return items.Select(r => new JobRunDto(r.Id, r.Name, r.StartedAt, r.FinishedAt, r.Status, r.Error)).ToList();
    }
}

public static class JobStates
{
    /// <summary>The state row of the job in the current tenant, created on first use.</summary>
    public static async Task<JobState> GetOrCreateAsync(
        IRepository<JobState, Guid> states,
        RecurringJobDefinition definition,
        JobSchedule schedule,
        TimeProvider clock,
        CancellationToken ct)
    {
        var state = await states.FirstOrDefaultAsync(new Specification<JobState>().Where(s => s.Name == definition.Name).Tracked(), ct);
        if (state is null)
        {
            state = JobState.Create(definition.Name, await schedule.NextAsync(definition.Cron, clock.GetUtcNow(), ct));
            states.Add(state);
        }

        return state;
    }
}

public sealed class JobsPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(JobsPermissions.Jobs.Manage, JobsPermissions.Module, "permissions.jobs.jobs.manage", "jobs"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("jobs.jobs", JobsPermissions.Module, "nav.jobs", "/jobs", "clock", JobsPermissions.Jobs.Manage, 820),
    ];
}
