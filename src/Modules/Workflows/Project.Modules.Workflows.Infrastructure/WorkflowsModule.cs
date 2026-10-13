using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Notifications;
using Project.Application.Tenancy;
using Project.Application.Workflows;
using Project.Infrastructure.Persistence;
using Project.Modules.Workflows.Application;
using Project.Modules.Workflows.Contracts;
using Project.Modules.Workflows.Domain;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Workflows.Infrastructure;

public sealed class WorkflowsDbContext(DbContextOptions<WorkflowsDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => "workflows";

    public DbSet<ApprovalRequest> Requests => Set<ApprovalRequest>();

    public DbSet<WorkflowDefinition> Definitions => Set<WorkflowDefinition>();
}

internal sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("Requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Workflow).HasMaxLength(128).IsRequired();
        builder.Property(r => r.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(r => r.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(256).IsRequired();
        builder.Property(r => r.Link).HasMaxLength(512);
        builder.Property(r => r.Status).HasMaxLength(16).IsRequired();
        builder.Property(r => r.Steps).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.Status, r.RequestedAt });
        builder.HasIndex(r => new { r.TenantId, r.EntityType, r.EntityId });
        builder.HasIndex(r => new { r.TenantId, r.RequestedBy });

        builder.HasMany(r => r.Decisions).WithOne().HasForeignKey(d => d.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Decisions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ApprovalDecisionConfiguration : IEntityTypeConfiguration<ApprovalDecision>
{
    public void Configure(EntityTypeBuilder<ApprovalDecision> builder)
    {
        builder.ToTable("Decisions");
        builder.HasKey(d => d.Id);

        // The id is assigned by the domain: a new decision added to the collection is an insert, not an update.
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Decision).HasMaxLength(16).IsRequired();
        builder.Property(d => d.Comment).HasMaxLength(1000);
    }
}

internal sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("Definitions");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Key).HasMaxLength(128).IsRequired();
        builder.Property(d => d.Steps).HasMaxLength(8000).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(64);
        builder.Property(d => d.UpdatedBy).HasMaxLength(64);
        builder.Property(d => d.DeletedBy).HasMaxLength(64);
        builder.HasIndex(d => new { d.TenantId, d.Key });
    }
}

public sealed record DecisionRequest(string? Comment);

public sealed record UpdateWorkflowRequest(List<ApprovalStep>? Steps);

/// <summary>
/// Approval flows for every module (<c>AddWorkflow</c> + <see cref="IApprovalService"/>): steps by permission, role
/// or user, conditional by amount, editable per tenant, with notifications and an inbox of pending decisions.
/// </summary>
public sealed class WorkflowsModule : IModule
{
    public string Name => "Workflows";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<WorkflowsDbContext>(Name, DataScope);
        services.AddRepositoriesFor<WorkflowsDbContext>();
        services.AddModuleConventions(typeof(WorkflowsPermissionProvider).Assembly);
        services.AddScoped<WorkflowCatalog>();
        services.AddScoped<ApproverResolver>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddNotificationTemplate(WorkflowTemplates.PendingTemplate);
        services.AddNotificationTemplate(WorkflowTemplates.CompletedTemplate);
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var requests = api.MapGroup("workflows/requests").WithTags("approvals").RequireAuthenticatedUser();
        requests.MapGet("/inbox", async (ISender sender, CancellationToken ct) => (await sender.Send(new ApprovalInboxQuery(), ct)).ToHttp())
            .WithName("ApprovalInbox");
        requests.MapGet("/mine", async (ISender sender, CancellationToken ct) => (await sender.Send(new MyApprovalRequestsQuery(), ct)).ToHttp())
            .WithName("MyApprovalRequests");
        requests.MapGet("/", async (string entityType, string entityId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new EntityApprovalRequestsQuery(entityType, entityId), ct)).ToHttp())
            .WithName("EntityApprovalRequests");
        requests.MapPost("/{id:guid}/approve", async (Guid id, DecisionRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DecideCommand(id, Decision.Approve, request.Comment), ct)).ToHttp())
            .WithName("ApproveRequest");
        requests.MapPost("/{id:guid}/reject", async (Guid id, DecisionRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DecideCommand(id, Decision.Reject, request.Comment), ct)).ToHttp())
            .WithName("RejectRequest");
        requests.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DecideCommand(id, Decision.Cancel, null), ct)).ToHttp())
            .WithName("CancelRequest");

        var definitions = api.MapGroup("workflows/definitions").WithTags("workflows").RequirePermission(WorkflowsPermissions.Definitions.Manage);
        definitions.MapGet("/", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListWorkflowsQuery(), ct)).ToHttp())
            .WithName("ListWorkflows");
        definitions.MapPut("/{key}", async (string key, UpdateWorkflowRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateWorkflowCommand(key, request.Steps), ct)).ToHttp())
            .WithName("UpdateWorkflow");
    }
}
