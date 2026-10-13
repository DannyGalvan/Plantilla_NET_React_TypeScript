using System.Text.Json;
using FluentValidation;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Notifications;
using Project.Application.Security;
using Project.Application.Workflows;
using Project.Modules.Identity.Contracts;
using Project.Modules.Workflows.Contracts;
using Project.Modules.Workflows.Domain;
using Project.SharedKernel;

namespace Project.Modules.Workflows.Application;

/// <summary>Steps of each flow for the current tenant: its own definition or the default of the module.</summary>
public sealed class WorkflowCatalog(IEnumerable<WorkflowTemplate> templates, IRepository<WorkflowDefinition, Guid> definitions)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    public IReadOnlyList<WorkflowTemplate> Templates { get; } = templates.OrderBy(t => t.Key, StringComparer.Ordinal).ToList();

    public WorkflowTemplate? Find(string key) => Templates.FirstOrDefault(t => t.Key == key);

    public async Task<IReadOnlyList<ApprovalStep>> StepsAsync(WorkflowTemplate template, CancellationToken ct)
    {
        var custom = await definitions.FirstOrDefaultAsync(new Specification<WorkflowDefinition>().Where(d => d.Key == template.Key), ct);
        return custom is null ? template.Steps : ParseSteps(custom.Steps);
    }

    public static IReadOnlyList<ApprovalStep> ParseSteps(string json) => JsonSerializer.Deserialize<List<ApprovalStep>>(json, Json) ?? [];
}

/// <summary>Who may decide on a step, and who should be told that it is waiting.</summary>
public sealed class ApproverResolver(ICurrentUser user, IPermissionService permissions, IUserDirectory directory)
{
    public async Task<bool> CanActAsync(ApprovalStep step, CancellationToken ct) => step.Kind switch
    {
        ApproverKind.Permission => await permissions.HasPermissionAsync(step.Approver, ct),
        ApproverKind.Role => user.Roles.Contains(step.Approver, StringComparer.OrdinalIgnoreCase),
        ApproverKind.User => string.Equals(user.UserId, step.Approver, StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    public async Task<IReadOnlyList<UserSummary>> ApproversAsync(ApprovalStep step, CancellationToken ct) => step.Kind switch
    {
        ApproverKind.Permission => await directory.FindWithPermissionAsync(step.Approver, ct),
        ApproverKind.Role => await directory.FindInRoleAsync(step.Approver, ct),
        ApproverKind.User when Guid.TryParse(step.Approver, out var id) => await directory.FindAsync([id], ct),
        _ => [],
    };
}

public sealed class ApprovalService(
    WorkflowCatalog catalog,
    IRepository<ApprovalRequest, Guid> requests,
    TimeProvider clock) : IApprovalService
{
    public async Task<Result<Guid>> SubmitAsync(ApprovalSubmission submission, CancellationToken ct)
    {
        var template = catalog.Find(submission.Workflow);
        if (template is null)
        {
            return Error.Validation("workflow", $"The workflow '{submission.Workflow}' is not declared.");
        }

        if (await requests.AnyAsync(Pending(submission.EntityType, submission.EntityId), ct))
        {
            return Error.Conflict("workflows.already_pending", "The record already has a pending approval request.");
        }

        var steps = (await catalog.StepsAsync(template, ct))
            .Where(s => s.MinAmount is null || (submission.Amount ?? 0) >= s.MinAmount)
            .ToList();
        var request = ApprovalRequest.Submit(
            submission.Workflow,
            submission.EntityType,
            submission.EntityId,
            submission.Title,
            submission.Link,
            submission.Amount,
            submission.RequestedBy,
            JsonSerializer.Serialize(steps, WorkflowCatalog.Json),
            steps.Count,
            clock.GetUtcNow());
        requests.Add(request);
        await requests.UnitOfWork.SaveChangesAsync(ct);
        return request.Id;
    }

    public async Task<ApprovalStatus?> GetStatusAsync(string entityType, string entityId, CancellationToken ct)
    {
        var latest = await requests.FirstOrDefaultAsync(
            new Specification<ApprovalRequest>().Where(r => r.EntityType == entityType && r.EntityId == entityId).OrderBy(r => r.RequestedAt, descending: true),
            ct);
        return latest is null ? null : Enum.Parse<ApprovalStatus>(latest.Status);
    }

    private static Specification<ApprovalRequest> Pending(string entityType, string entityId) =>
        new Specification<ApprovalRequest>().Where(r => r.EntityType == entityType && r.EntityId == entityId && r.Status == ApprovalRequest.Statuses.Pending);
}

// ----- Reactions (dispatched by the outbox, inside the tenant) -----

/// <summary>Tells the approvers of the current step that a request is waiting for them.</summary>
public sealed class ApprovalRequestAdvancedHandler(
    IRepository<ApprovalRequest, Guid> requests,
    ApproverResolver approvers,
    INotificationSender notifications) : IDomainEventHandler<ApprovalRequestAdvanced>
{
    public async Task HandleAsync(ApprovalRequestAdvanced domainEvent, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(domainEvent.RequestId, ct);
        if (request is null || request.Status != ApprovalRequest.Statuses.Pending)
        {
            return;
        }

        var step = WorkflowCatalog.ParseSteps(request.Steps)[request.CurrentStep];
        var recipients = (await approvers.ApproversAsync(step, ct)).Select(u => u.Id).Where(id => id != request.RequestedBy).ToList();
        if (recipients.Count > 0)
        {
            await notifications.SendAsync(
                new Notify(WorkflowTemplates.Pending, recipients, new Dictionary<string, string> { ["title"] = request.Title, ["step"] = step.Name }, "/workflows/inbox", Email: true),
                ct);
        }
    }
}

/// <summary>Publishes the outcome to the owning module and tells the requester.</summary>
public sealed class ApprovalRequestCompletedHandler(
    IRepository<ApprovalRequest, Guid> requests,
    IEventBus bus,
    INotificationSender notifications) : IDomainEventHandler<ApprovalRequestCompleted>
{
    public async Task HandleAsync(ApprovalRequestCompleted domainEvent, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(domainEvent.RequestId, ct);
        if (request is null)
        {
            return;
        }

        var status = Enum.Parse<ApprovalStatus>(domainEvent.Status);
        await bus.PublishAsync(
            new ApprovalCompletedEvent(request.Id, request.Workflow, request.EntityType, request.EntityId, status, domainEvent.Comment, domainEvent.DecidedBy?.ToString()),
            ct);

        if (status != ApprovalStatus.Cancelled)
        {
            await notifications.SendAsync(
                new Notify(WorkflowTemplates.Completed, [request.RequestedBy], new Dictionary<string, string> { ["title"] = request.Title, ["status"] = status.ToString() }, request.Link, Email: true),
                ct);
        }
    }
}

// ----- Queries and commands -----

public sealed record ApprovalDecisionDto(int Step, string Decision, Guid UserId, string? Comment, DateTimeOffset At);

public sealed record ApprovalRequestDto(
    Guid Id,
    string Workflow,
    string EntityType,
    string EntityId,
    string Title,
    string? Link,
    decimal? Amount,
    Guid RequestedBy,
    DateTimeOffset RequestedAt,
    string Status,
    int CurrentStep,
    IReadOnlyList<ApprovalStep> Steps,
    IReadOnlyList<ApprovalDecisionDto> Decisions,
    bool CanDecide)
{
    public static ApprovalRequestDto From(ApprovalRequest r, bool canDecide) =>
        new(
            r.Id,
            r.Workflow,
            r.EntityType,
            r.EntityId,
            r.Title,
            r.Link,
            r.Amount,
            r.RequestedBy,
            r.RequestedAt,
            r.Status,
            r.CurrentStep,
            WorkflowCatalog.ParseSteps(r.Steps),
            r.Decisions.OrderBy(d => d.At).Select(d => new ApprovalDecisionDto(d.Step, d.Decision, d.UserId, d.Comment, d.At)).ToList(),
            canDecide);
}

internal static class RequestAccess
{
    public static Guid? Me(ICurrentUser user) => Guid.TryParse(user.UserId, out var id) ? id : null;

    public static async Task<bool> CanDecideAsync(ApprovalRequest request, ApproverResolver approvers, Guid me, CancellationToken ct) =>
        request.Status == ApprovalRequest.Statuses.Pending
        && request.RequestedBy != me
        && await approvers.CanActAsync(WorkflowCatalog.ParseSteps(request.Steps)[request.CurrentStep], ct);

    public static Specification<ApprovalRequest> WithDecisions() => new Specification<ApprovalRequest>().Include(nameof(ApprovalRequest.Decisions));
}

/// <summary>Pending requests the caller can decide on now.</summary>
public sealed record ApprovalInboxQuery : IQuery<IReadOnlyList<ApprovalRequestDto>>;

public sealed class ApprovalInboxHandler(IRepository<ApprovalRequest, Guid> requests, ApproverResolver approvers, ICurrentUser user)
    : IQueryHandler<ApprovalInboxQuery, IReadOnlyList<ApprovalRequestDto>>
{
    public async ValueTask<Result<IReadOnlyList<ApprovalRequestDto>>> Handle(ApprovalInboxQuery query, CancellationToken cancellationToken)
    {
        if (RequestAccess.Me(user) is not { } me)
        {
            return Error.Unauthorized("auth.unauthorized", "Sign in first.");
        }

        var pending = await requests.ListAsync(
            RequestAccess.WithDecisions().Where(r => r.Status == ApprovalRequest.Statuses.Pending && r.RequestedBy != me).OrderBy(r => r.RequestedAt).Page(0, 500),
            cancellationToken);
        var mine = new List<ApprovalRequestDto>();
        foreach (var request in pending)
        {
            if (await RequestAccess.CanDecideAsync(request, approvers, me, cancellationToken))
            {
                mine.Add(ApprovalRequestDto.From(request, canDecide: true));
            }
        }

        return mine;
    }
}

/// <summary>Requests submitted by the caller, newest first.</summary>
public sealed record MyApprovalRequestsQuery : IQuery<IReadOnlyList<ApprovalRequestDto>>;

public sealed class MyApprovalRequestsHandler(IRepository<ApprovalRequest, Guid> requests, ICurrentUser user)
    : IQueryHandler<MyApprovalRequestsQuery, IReadOnlyList<ApprovalRequestDto>>
{
    public async ValueTask<Result<IReadOnlyList<ApprovalRequestDto>>> Handle(MyApprovalRequestsQuery query, CancellationToken cancellationToken)
    {
        if (RequestAccess.Me(user) is not { } me)
        {
            return Error.Unauthorized("auth.unauthorized", "Sign in first.");
        }

        var items = await requests.ListAsync(
            RequestAccess.WithDecisions().Where(r => r.RequestedBy == me).OrderBy(r => r.RequestedAt, descending: true).Page(0, 100),
            cancellationToken);
        return items.Select(r => ApprovalRequestDto.From(r, canDecide: false)).ToList();
    }
}

/// <summary>Requests of a record, visible to the requester, its approvers and holders of <c>workflows.requests.view-all</c>.</summary>
public sealed record EntityApprovalRequestsQuery(string EntityType, string EntityId) : IQuery<IReadOnlyList<ApprovalRequestDto>>;

public sealed class EntityApprovalRequestsHandler(
    IRepository<ApprovalRequest, Guid> requests,
    ApproverResolver approvers,
    IPermissionService permissions,
    ICurrentUser user) : IQueryHandler<EntityApprovalRequestsQuery, IReadOnlyList<ApprovalRequestDto>>
{
    public async ValueTask<Result<IReadOnlyList<ApprovalRequestDto>>> Handle(EntityApprovalRequestsQuery query, CancellationToken cancellationToken)
    {
        if (RequestAccess.Me(user) is not { } me)
        {
            return Error.Unauthorized("auth.unauthorized", "Sign in first.");
        }

        var all = await permissions.HasPermissionAsync(WorkflowsPermissions.Requests.ViewAll, cancellationToken);
        var items = await requests.ListAsync(
            RequestAccess.WithDecisions().Where(r => r.EntityType == query.EntityType && r.EntityId == query.EntityId).OrderBy(r => r.RequestedAt, descending: true),
            cancellationToken);

        var visible = new List<ApprovalRequestDto>();
        foreach (var request in items)
        {
            var canDecide = await RequestAccess.CanDecideAsync(request, approvers, me, cancellationToken);
            if (all || canDecide || request.RequestedBy == me || request.Decisions.Any(d => d.UserId == me))
            {
                visible.Add(ApprovalRequestDto.From(request, canDecide));
            }
        }

        return visible;
    }
}

public enum Decision
{
    Approve,
    Reject,
    Cancel,
}

public sealed record DecideCommand(Guid RequestId, Decision Decision, string? Comment) : ICommand<ApprovalRequestDto>;

public sealed class DecideValidator : AbstractValidator<DecideCommand>
{
    public DecideValidator()
    {
        RuleFor(c => c.Comment).MaximumLength(1000);
        RuleFor(c => c.Comment).NotEmpty().When(c => c.Decision == Decision.Reject).WithMessage("Explain why the request is rejected.");
    }
}

public sealed class DecideHandler(
    IRepository<ApprovalRequest, Guid> requests,
    ApproverResolver approvers,
    IPermissionService permissions,
    ICurrentUser user,
    TimeProvider clock) : ICommandHandler<DecideCommand, ApprovalRequestDto>
{
    public async ValueTask<Result<ApprovalRequestDto>> Handle(DecideCommand command, CancellationToken cancellationToken)
    {
        if (RequestAccess.Me(user) is not { } me)
        {
            return Error.Unauthorized("auth.unauthorized", "Sign in first.");
        }

        var request = await requests.FirstOrDefaultAsync(RequestAccess.WithDecisions().Where(r => r.Id == command.RequestId).Tracked(), cancellationToken);
        if (request is null)
        {
            return Error.NotFound("workflows.request_not_found");
        }

        var now = clock.GetUtcNow();
        Result result;
        if (command.Decision == Decision.Cancel)
        {
            result = request.Cancel(me, now);
        }
        else
        {
            var step = WorkflowCatalog.ParseSteps(request.Steps).ElementAtOrDefault(request.CurrentStep);
            if (step is null || !await approvers.CanActAsync(step, cancellationToken))
            {
                // Requests the caller cannot act on are not revealed.
                var visible = request.RequestedBy == me || await permissions.HasPermissionAsync(WorkflowsPermissions.Requests.ViewAll, cancellationToken);
                return visible ? Error.Forbidden("workflows.not_approver", "You are not an approver of the current step.") : Error.NotFound("workflows.request_not_found");
            }

            result = command.Decision == Decision.Approve ? request.Approve(me, command.Comment, now) : request.Reject(me, command.Comment, now);
        }

        if (result.IsFailure)
        {
            return result.Error!;
        }

        await requests.UnitOfWork.SaveChangesAsync(cancellationToken);
        return ApprovalRequestDto.From(request, await RequestAccess.CanDecideAsync(request, approvers, me, cancellationToken));
    }
}

// ----- Definitions per tenant -----

public sealed record WorkflowDto(string Key, string Module, string Name, IReadOnlyList<ApprovalStep> DefaultSteps, IReadOnlyList<ApprovalStep> Steps, bool Customized);

public sealed record ListWorkflowsQuery : IQuery<IReadOnlyList<WorkflowDto>>, IRequirePermission
{
    public string Permission => WorkflowsPermissions.Definitions.Manage;
}

public sealed class ListWorkflowsHandler(WorkflowCatalog catalog, IRepository<WorkflowDefinition, Guid> definitions)
    : IQueryHandler<ListWorkflowsQuery, IReadOnlyList<WorkflowDto>>
{
    public async ValueTask<Result<IReadOnlyList<WorkflowDto>>> Handle(ListWorkflowsQuery query, CancellationToken cancellationToken)
    {
        var custom = await definitions.ListAsync(new Specification<WorkflowDefinition>(), cancellationToken);
        return catalog.Templates
            .Select(t =>
            {
                var own = custom.FirstOrDefault(c => c.Key == t.Key);
                return new WorkflowDto(t.Key, t.Module, t.Name, t.Steps, own is null ? t.Steps : WorkflowCatalog.ParseSteps(own.Steps), own is not null);
            })
            .ToList();
    }
}

/// <summary>Replaces the steps of a flow for the tenant. Null steps restore the default.</summary>
public sealed record UpdateWorkflowCommand(string Key, List<ApprovalStep>? Steps) : ICommand, IRequirePermission
{
    public string Permission => WorkflowsPermissions.Definitions.Manage;
}

public sealed class UpdateWorkflowValidator : AbstractValidator<UpdateWorkflowCommand>
{
    public UpdateWorkflowValidator()
    {
        RuleFor(c => c.Steps).Must(s => s is null || s.Count is > 0 and <= 10).WithMessage("A flow has between 1 and 10 steps.");
        RuleForEach(c => c.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Name).NotEmpty().MaximumLength(64);
            step.RuleFor(s => s.Approver).NotEmpty().MaximumLength(128);
            step.RuleFor(s => s.Kind).IsInEnum();
            step.RuleFor(s => s.Approver).Must(a => Guid.TryParse(a, out _)).When(s => s.Kind == ApproverKind.User).WithMessage("The approver must be a user id.");
            step.RuleFor(s => s.MinAmount).GreaterThanOrEqualTo(0).When(s => s.MinAmount is not null);
        });
    }
}

public sealed class UpdateWorkflowHandler(WorkflowCatalog catalog, IRepository<WorkflowDefinition, Guid> definitions)
    : ICommandHandler<UpdateWorkflowCommand>
{
    public async ValueTask<Result> Handle(UpdateWorkflowCommand command, CancellationToken cancellationToken)
    {
        if (catalog.Find(command.Key) is null)
        {
            return Error.NotFound("workflows.not_found");
        }

        var current = await definitions.FirstOrDefaultAsync(new Specification<WorkflowDefinition>().Where(d => d.Key == command.Key).Tracked(), cancellationToken);
        if (command.Steps is null)
        {
            if (current is not null)
            {
                definitions.Remove(current);
            }
        }
        else
        {
            var json = JsonSerializer.Serialize(command.Steps, WorkflowCatalog.Json);
            if (current is null)
            {
                definitions.Add(new WorkflowDefinition { Key = command.Key, Steps = json });
            }
            else
            {
                current.Steps = json;
            }
        }

        await definitions.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public static class WorkflowTemplates
{
    public const string Pending = "workflows.approval-pending";
    public const string Completed = "workflows.request-completed";

    public static NotificationTemplate PendingTemplate { get; } = new(
        Pending,
        WorkflowsPermissions.Module,
        new Dictionary<string, LocalizedTemplate>
        {
            ["es"] = new("Aprobación pendiente: {{title}}", "Tienes una solicitud esperando tu decisión en el paso {{step}}: {{title}}."),
            ["en"] = new("Approval pending: {{title}}", "A request is waiting for your decision at step {{step}}: {{title}}."),
        },
        ["title", "step"]);

    public static NotificationTemplate CompletedTemplate { get; } = new(
        Completed,
        WorkflowsPermissions.Module,
        new Dictionary<string, LocalizedTemplate>
        {
            ["es"] = new("Solicitud {{status}}: {{title}}", "Tu solicitud \"{{title}}\" terminó con estado {{status}}."),
            ["en"] = new("Request {{status}}: {{title}}", "Your request \"{{title}}\" finished as {{status}}."),
        },
        ["title", "status"]);
}

public sealed class WorkflowsPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(WorkflowsPermissions.Definitions.Manage, WorkflowsPermissions.Module, "permissions.workflows.definitions.manage", "definitions"),
        new(WorkflowsPermissions.Requests.ViewAll, WorkflowsPermissions.Module, "permissions.workflows.requests.view-all", "requests"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("workflows.inbox", WorkflowsPermissions.Module, "nav.approvals", "/workflows/inbox", "check-square", null, 50),
        new("workflows.definitions", WorkflowsPermissions.Module, "nav.workflows", "/workflows/definitions", "git-branch", WorkflowsPermissions.Definitions.Manage, 840),
    ];
}
