using Project.SharedKernel.Domain;

namespace Project.Modules.Jobs.Domain;

/// <summary>Schedule and lease of one recurring job in one tenant.</summary>
public sealed class JobState : AggregateRoot<Guid>, IMultiTenant
{
    private JobState()
    {
    }

    public Guid TenantId { get; set; }

    public string Name { get; private set; } = string.Empty;

    public bool Enabled { get; private set; } = true;

    /// <summary>Schedule chosen by the tenant; null uses the default of the module.</summary>
    public string? Cron { get; private set; }

    public DateTimeOffset NextRunAt { get; private set; }

    /// <summary>Lease: only the instance that owns it runs the job.</summary>
    public Guid? LockId { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? LastStartedAt { get; private set; }

    public DateTimeOffset? LastFinishedAt { get; private set; }

    public string? LastStatus { get; private set; }

    public string? LastError { get; private set; }

    public static JobState Create(string name, DateTimeOffset nextRunAt) => new() { Id = NewId.Next(), Name = name, NextRunAt = nextRunAt };

    public void Configure(bool enabled, string? cron, DateTimeOffset nextRunAt)
    {
        Enabled = enabled;
        Cron = string.IsNullOrWhiteSpace(cron) ? null : cron.Trim();
        NextRunAt = nextRunAt;
    }

    public void RunNow(DateTimeOffset now) => NextRunAt = now;

    public void Finish(string status, string? error, DateTimeOffset startedAt, DateTimeOffset finishedAt, DateTimeOffset nextRunAt)
    {
        LastStatus = status;
        LastError = error is { Length: > 2000 } ? error[..2000] : error;
        LastStartedAt = startedAt;
        LastFinishedAt = finishedAt;
        NextRunAt = nextRunAt;
        LockId = null;
        LockedUntil = null;
    }
}

/// <summary>One execution of a job, kept for the history page.</summary>
public sealed class JobRun : AggregateRoot<Guid>, IMultiTenant
{
    private JobRun()
    {
    }

    public Guid TenantId { get; set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset FinishedAt { get; private set; }

    public string Status { get; private set; } = string.Empty;

    public string? Error { get; private set; }

    public static JobRun Create(string name, DateTimeOffset startedAt, DateTimeOffset finishedAt, string status, string? error) =>
        new()
        {
            Id = NewId.Next(),
            Name = name,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            Status = status,
            Error = error is { Length: > 2000 } ? error[..2000] : error,
        };
}

public static class JobStatuses
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}
