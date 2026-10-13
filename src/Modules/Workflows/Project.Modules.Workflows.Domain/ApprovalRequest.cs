using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Modules.Workflows.Domain;

/// <summary>
/// A request moving through the steps of a flow. The steps are copied at submission (<see cref="Steps"/>, JSON), so
/// changing the definition later never alters requests in progress.
/// </summary>
public sealed class ApprovalRequest : AggregateRoot<Guid>, IMultiTenant
{
    private readonly List<ApprovalDecision> _decisions = [];

    private ApprovalRequest()
    {
    }

    public Guid TenantId { get; set; }

    public string Workflow { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Link { get; private set; }

    public decimal? Amount { get; private set; }

    public Guid RequestedBy { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Pending, Approved, Rejected or Cancelled.</summary>
    public string Status { get; private set; } = Statuses.Pending;

    /// <summary>JSON array of the steps that apply to this request.</summary>
    public string Steps { get; private set; } = "[]";

    public int StepCount { get; private set; }

    /// <summary>Zero-based index of the step waiting for a decision.</summary>
    public int CurrentStep { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<ApprovalDecision> Decisions => _decisions;

    public static ApprovalRequest Submit(
        string workflow,
        string entityType,
        string entityId,
        string title,
        string? link,
        decimal? amount,
        Guid requestedBy,
        string steps,
        int stepCount,
        DateTimeOffset now)
    {
        var request = new ApprovalRequest
        {
            Id = NewId.Next(),
            Workflow = workflow,
            EntityType = entityType,
            EntityId = entityId,
            Title = title.Length > 256 ? title[..256] : title,
            Link = link,
            Amount = amount,
            RequestedBy = requestedBy,
            RequestedAt = now,
            Steps = steps,
            StepCount = stepCount,
        };

        // A flow without applicable steps (all conditional and below the amount) is approved right away.
        if (stepCount == 0)
        {
            request.Complete(Statuses.Approved, null, null, now);
        }
        else
        {
            request.Raise(new ApprovalRequestAdvanced(request.Id));
        }

        return request;
    }

    public Result Approve(Guid userId, string? comment, DateTimeOffset now)
    {
        if (Check(userId) is { } problem)
        {
            return problem;
        }

        _decisions.Add(new ApprovalDecision(Id, CurrentStep, Statuses.Approved, userId, comment, now));
        CurrentStep++;
        if (CurrentStep >= StepCount)
        {
            Complete(Statuses.Approved, userId, comment, now);
        }
        else
        {
            Raise(new ApprovalRequestAdvanced(Id));
        }

        return Result.Success();
    }

    public Result Reject(Guid userId, string? comment, DateTimeOffset now)
    {
        if (Check(userId) is { } problem)
        {
            return problem;
        }

        _decisions.Add(new ApprovalDecision(Id, CurrentStep, Statuses.Rejected, userId, comment, now));
        Complete(Statuses.Rejected, userId, comment, now);
        return Result.Success();
    }

    public Result Cancel(Guid userId, DateTimeOffset now)
    {
        if (Status != Statuses.Pending)
        {
            return Error.Conflict("workflows.already_completed", "The request is no longer pending.");
        }

        if (userId != RequestedBy)
        {
            return Error.Forbidden("workflows.not_requester", "Only the requester can cancel the request.");
        }

        Complete(Statuses.Cancelled, userId, null, now);
        return Result.Success();
    }

    private Error? Check(Guid userId)
    {
        if (Status != Statuses.Pending)
        {
            return Error.Conflict("workflows.already_completed", "The request is no longer pending.");
        }

        // Separation of duties: nobody approves his own request.
        return userId == RequestedBy ? Error.Forbidden("workflows.own_request", "You cannot decide on your own request.") : null;
    }

    private void Complete(string status, Guid? userId, string? comment, DateTimeOffset now)
    {
        Status = status;
        CompletedAt = now;
        Raise(new ApprovalRequestCompleted(Id, status, comment, userId));
    }

    public static class Statuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
    }
}

public sealed class ApprovalDecision
{
    private ApprovalDecision()
    {
    }

    internal ApprovalDecision(Guid requestId, int step, string decision, Guid userId, string? comment, DateTimeOffset at)
    {
        Id = NewId.Next();
        RequestId = requestId;
        Step = step;
        Decision = decision;
        UserId = userId;
        Comment = comment is { Length: > 1000 } ? comment[..1000] : comment;
        At = at;
    }

    public Guid Id { get; private set; }

    public Guid RequestId { get; private set; }

    public int Step { get; private set; }

    public string Decision { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }

    public string? Comment { get; private set; }

    public DateTimeOffset At { get; private set; }
}

/// <summary>A step is waiting for a decision (after submission or after the previous approval): notify its approvers.</summary>
public sealed record ApprovalRequestAdvanced(Guid RequestId) : IDomainEvent;

/// <summary>The request finished (published to the owning module as ApprovalCompletedEvent).</summary>
public sealed record ApprovalRequestCompleted(Guid RequestId, string Status, string? Comment, Guid? DecidedBy) : IDomainEvent;

/// <summary>Steps of a flow chosen by the tenant (JSON); replaces the default steps of the module.</summary>
public sealed class WorkflowDefinition : AuditableAggregateRoot, IMultiTenant
{
    public Guid TenantId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Steps { get; set; } = "[]";
}
