using Microsoft.Extensions.DependencyInjection;
using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Application.Workflows;

public enum ApproverKind
{
    /// <summary>Any user holding the permission (for example <c>hr.leave.approve</c>).</summary>
    Permission,

    /// <summary>Any user assigned to the role (by name).</summary>
    Role,

    /// <summary>One specific user (id).</summary>
    User,
}

/// <summary>A step of an approval flow. With <see cref="MinAmount"/> the step only applies from that amount on.</summary>
public sealed record ApprovalStep(string Name, ApproverKind Kind, string Approver, decimal? MinAmount = null);

/// <summary>Default definition of a flow, declared by the module that owns it. Tenants may replace the steps.</summary>
public sealed record WorkflowTemplate(string Key, string Module, string Name, IReadOnlyList<ApprovalStep> Steps);

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}

/// <summary>
/// Asks for approval of a business record: leave requests, purchases, expenses, price changes, job offers.
/// <see cref="Amount"/> decides which conditional steps apply; <see cref="Link"/> is the page approvers open.
/// </summary>
public sealed record ApprovalSubmission(
    string Workflow,
    string EntityType,
    string EntityId,
    string Title,
    Guid RequestedBy,
    decimal? Amount = null,
    string? Link = null);

/// <summary>Raised (through the outbox, at least once) when a request is approved, rejected or cancelled.</summary>
public sealed record ApprovalCompletedEvent(
    Guid RequestId,
    string Workflow,
    string EntityType,
    string EntityId,
    ApprovalStatus Status,
    string? Comment,
    string? DecidedBy) : IDomainEvent;

/// <summary>
/// Approval flows for any module. The owning module submits requests and reacts to
/// <see cref="ApprovalCompletedEvent"/> with an <c>IDomainEventHandler</c> filtered by workflow.
/// </summary>
public interface IApprovalService
{
    Task<Result<Guid>> SubmitAsync(ApprovalSubmission submission, CancellationToken ct);

    /// <summary>Status of the latest request of the record, or null when it was never submitted.</summary>
    Task<ApprovalStatus?> GetStatusAsync(string entityType, string entityId, CancellationToken ct);
}

public static class WorkflowExtensions
{
    /// <summary>
    /// Declares a flow with its default steps:
    /// <code>
    /// services.AddWorkflow(new("hr.leave", "hr", "Leave request",
    /// [
    ///     new("Manager", ApproverKind.Permission, "hr.leave.approve"),
    ///     new("HR", ApproverKind.Role, "HR", MinAmount: 10), // only for 10 days or more
    /// ]));
    /// </code>
    /// </summary>
    public static IServiceCollection AddWorkflow(this IServiceCollection services, WorkflowTemplate template) =>
        services.AddSingleton(template);
}
