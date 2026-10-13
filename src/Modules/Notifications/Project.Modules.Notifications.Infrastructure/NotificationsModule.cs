using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Project.Application;
using Project.Application.Configuration;
using Project.Application.Jobs;
using Project.Application.Notifications;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Notifications.Application;
using Project.Modules.Notifications.Contracts;
using Project.Modules.Notifications.Domain;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Notifications.Infrastructure;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => "notifications";

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationTemplateOverride> TemplateOverrides => Set<NotificationTemplateOverride>();
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Template).HasMaxLength(128).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(256).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(4000).IsRequired();
        builder.Property(n => n.Link).HasMaxLength(512);
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.ReadAt });
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.CreatedAt });
    }
}

internal sealed class NotificationTemplateOverrideConfiguration : IEntityTypeConfiguration<NotificationTemplateOverride>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateOverride> builder)
    {
        builder.ToTable("TemplateOverrides");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Key).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Language).HasMaxLength(8).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(256).IsRequired();
        builder.Property(t => t.Body).HasMaxLength(4000).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(64);
        builder.Property(t => t.UpdatedBy).HasMaxLength(64);
        builder.Property(t => t.DeletedBy).HasMaxLength(64);
        builder.HasIndex(t => new { t.TenantId, t.Key, t.Language });
    }
}

public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Read notifications older than this are deleted by the <c>notifications.cleanup</c> job. 0 keeps them.</summary>
    public int RetentionDays { get; set; } = 90;
}

/// <summary>Deletes read notifications older than <c>Notifications:RetentionDays</c>.</summary>
public sealed class NotificationsCleanupJob(NotificationsDbContext db, IOptionsSnapshot<NotificationsOptions> options, TimeProvider clock) : IRecurringJob
{
    public const string Name = "notifications.cleanup";

    public async Task ExecuteAsync(CancellationToken ct)
    {
        if (options.Value.RetentionDays > 0)
        {
            var cutoff = clock.GetUtcNow().AddDays(-options.Value.RetentionDays);
            await db.Notifications.Where(n => n.ReadAt != null && n.ReadAt < cutoff).ExecuteDeleteAsync(ct);
        }
    }
}

public sealed record UpdateTemplateRequest(string? Subject, string? Body);

/// <summary>
/// In-app inbox and email notifications for every module. Modules declare templates with
/// <c>AddNotificationTemplate</c> and send with <see cref="INotificationSender"/>; tenants customize the texts.
/// </summary>
public sealed class NotificationsModule : IModule
{
    public string Name => "Notifications";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<NotificationsDbContext>(Name, DataScope);
        services.AddRepositoriesFor<NotificationsDbContext>();
        services.AddModuleConventions(typeof(NotificationsPermissionProvider).Assembly);
        services.AddScoped<TemplateRenderer>();
        services.AddScoped<INotificationSender, NotificationSender>();
        services.AddNotificationTemplate(NotificationTemplates.TestTemplate);
        services.AddOptions<NotificationsOptions>().Bind(configuration.GetSection(NotificationsOptions.SectionName));
        services.AddConfigurableSection<NotificationsOptions>(NotificationsOptions.SectionName, "notifications", "Retención de notificaciones leídas.");
        services.AddRecurringJob<NotificationsCleanupJob>(NotificationsCleanupJob.Name, "15 4 * * *", description: "Deletes old read notifications.");
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var inbox = api.MapGroup("me/notifications").WithTags("notifications").RequireAuthenticatedUser();
        inbox.MapGet("/", async ([AsParameters] ListMyNotificationsQuery query, ISender sender, CancellationToken ct) => (await sender.Send(query, ct)).ToHttp())
            .WithName("ListMyNotifications");
        inbox.MapGet("/unread-count", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CountUnreadQuery(), ct);
                return result.IsFailure ? result.ToHttp() : Results.Ok(new { count = result.Value });
            })
            .WithName("CountUnreadNotifications");
        inbox.MapPost("/{id:guid}/read", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new MarkReadCommand(id), ct)).ToHttp())
            .WithName("MarkNotificationRead");
        inbox.MapPost("/read-all", async (ISender sender, CancellationToken ct) => (await sender.Send(new MarkReadCommand(null), ct)).ToHttp())
            .WithName("MarkAllNotificationsRead");

        var templates = api.MapGroup("notifications/templates").WithTags("notification-templates");
        templates.MapGet("/", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListTemplatesQuery(), ct)).ToHttp())
            .RequirePermission(NotificationsPermissions.Templates.Manage)
            .WithName("ListNotificationTemplates");
        templates.MapPut("/{key}/{language}", async (string key, string language, UpdateTemplateRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateTemplateCommand(key, language, request.Subject, request.Body), ct)).ToHttp())
            .RequirePermission(NotificationsPermissions.Templates.Manage)
            .WithName("UpdateNotificationTemplate");
        api.MapPost("notifications/test", async (ISender sender, CancellationToken ct) => (await sender.Send(new SendTestNotificationCommand(), ct)).ToHttp())
            .RequirePermission(NotificationsPermissions.Templates.Manage)
            .WithTags("notification-templates")
            .WithName("SendTestNotification");
    }
}
