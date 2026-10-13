using FluentValidation;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Notifications;
using Project.Application.Security;
using Project.Modules.Notifications.Contracts;
using Project.Modules.Notifications.Domain;
using Project.SharedKernel;

namespace Project.Modules.Notifications.Application;

public sealed record NotificationDto(Guid Id, string Template, string Title, string Body, string? Link, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt)
{
    public static NotificationDto From(Notification n) => new(n.Id, n.Template, n.Title, n.Body, n.Link, n.CreatedAt, n.ReadAt);
}

internal static class Me
{
    public static Guid? Id(ICurrentUser user) => Guid.TryParse(user.UserId, out var id) ? id : null;
}

public sealed record ListMyNotificationsQuery(bool UnreadOnly = false, int Page = 1, int PageSize = 20) : IQuery<PagedResult<NotificationDto>>;

public sealed class ListMyNotificationsHandler(IRepository<Notification, Guid> notifications, ICurrentUser user)
    : IQueryHandler<ListMyNotificationsQuery, PagedResult<NotificationDto>>
{
    public async ValueTask<Result<PagedResult<NotificationDto>>> Handle(ListMyNotificationsQuery query, CancellationToken cancellationToken)
    {
        if (Me.Id(user) is not { } me)
        {
            return Error.Unauthorized("auth.unauthorized", "Sign in first.");
        }

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);
        var spec = new Specification<Notification>().Where(n => n.UserId == me);
        if (query.UnreadOnly)
        {
            spec.Where(n => n.ReadAt == null);
        }

        var total = await notifications.CountAsync(spec, cancellationToken);
        var items = await notifications.ListAsync(spec.OrderBy(n => n.CreatedAt, descending: true).Page((page - 1) * size, size), cancellationToken);
        return new PagedResult<NotificationDto>(items.Select(NotificationDto.From).ToList(), page, size, total);
    }
}

public sealed record CountUnreadQuery : IQuery<int>;

public sealed class CountUnreadHandler(IRepository<Notification, Guid> notifications, ICurrentUser user) : IQueryHandler<CountUnreadQuery, int>
{
    public async ValueTask<Result<int>> Handle(CountUnreadQuery query, CancellationToken cancellationToken) =>
        Me.Id(user) is { } me
            ? await notifications.CountAsync(new Specification<Notification>().Where(n => n.UserId == me && n.ReadAt == null), cancellationToken)
            : 0;
}

/// <summary>Marks one notification (or all of them when <see cref="Id"/> is null) as read.</summary>
public sealed record MarkReadCommand(Guid? Id) : ICommand;

public sealed class MarkReadHandler(IRepository<Notification, Guid> notifications, ICurrentUser user, TimeProvider clock) : ICommandHandler<MarkReadCommand>
{
    public async ValueTask<Result> Handle(MarkReadCommand command, CancellationToken cancellationToken)
    {
        if (Me.Id(user) is not { } me)
        {
            return Error.Unauthorized("auth.unauthorized", "Sign in first.");
        }

        var spec = new Specification<Notification>().Where(n => n.UserId == me && n.ReadAt == null).Tracked();
        if (command.Id is { } id)
        {
            spec.Where(n => n.Id == id);
        }

        var unread = await notifications.ListAsync(spec.Page(0, 500), cancellationToken);
        if (command.Id is not null && unread.Count == 0)
        {
            return Result.Success();
        }

        var now = clock.GetUtcNow();
        foreach (var notification in unread)
        {
            notification.MarkRead(now);
        }

        await notifications.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Sends a sample notification (and email) to the caller, to check the templates and the SMTP settings.</summary>
public sealed record SendTestNotificationCommand : ICommand, IRequirePermission
{
    public string Permission => NotificationsPermissions.Templates.Manage;
}

public sealed class SendTestNotificationHandler(INotificationSender sender, ICurrentUser user) : ICommandHandler<SendTestNotificationCommand>
{
    public async ValueTask<Result> Handle(SendTestNotificationCommand command, CancellationToken cancellationToken)
    {
        if (Me.Id(user) is not { } me)
        {
            return Error.Unauthorized("auth.unauthorized", "Sign in first.");
        }

        await sender.SendAsync(
            new Notify(NotificationTemplates.Test, [me], new Dictionary<string, string> { ["user"] = user.UserName ?? string.Empty }, "/", Email: true),
            cancellationToken);
        return Result.Success();
    }
}

// ----- Templates (tenant overrides) -----

public sealed record TemplateDto(string Key, string Module, string Language, string Subject, string Body, bool Customized, IReadOnlyList<string> Placeholders);

public sealed record ListTemplatesQuery : IQuery<IReadOnlyList<TemplateDto>>, IRequirePermission
{
    public string Permission => NotificationsPermissions.Templates.Manage;
}

public sealed class ListTemplatesHandler(TemplateRenderer renderer, IRepository<NotificationTemplateOverride, Guid> overrides)
    : IQueryHandler<ListTemplatesQuery, IReadOnlyList<TemplateDto>>
{
    public async ValueTask<Result<IReadOnlyList<TemplateDto>>> Handle(ListTemplatesQuery query, CancellationToken cancellationToken)
    {
        var custom = await overrides.ListAsync(new Specification<NotificationTemplateOverride>(), cancellationToken);
        IReadOnlyList<TemplateDto> items =
        [
            .. from template in renderer.Declared
               from language in template.Languages
               let changed = custom.FirstOrDefault(c => c.Key == template.Key && c.Language == language.Key)
               select new TemplateDto(
                   template.Key,
                   template.Module,
                   language.Key,
                   changed?.Subject ?? language.Value.Subject,
                   changed?.Body ?? language.Value.Body,
                   changed is not null,
                   template.Placeholders),
        ];
        return Result<IReadOnlyList<TemplateDto>>.Success(items);
    }
}

/// <summary>Changes the text of a template for the tenant. An empty subject and body restore the default.</summary>
public sealed record UpdateTemplateCommand(string Key, string Language, string? Subject, string? Body) : ICommand, IRequirePermission
{
    public string Permission => NotificationsPermissions.Templates.Manage;
}

public sealed class UpdateTemplateValidator : AbstractValidator<UpdateTemplateCommand>
{
    public UpdateTemplateValidator()
    {
        RuleFor(c => c.Language).NotEmpty().MaximumLength(8);
        RuleFor(c => c.Subject).MaximumLength(256);
        RuleFor(c => c.Body).MaximumLength(4000);
    }
}

public sealed class UpdateTemplateHandler(TemplateRenderer renderer, IRepository<NotificationTemplateOverride, Guid> overrides)
    : ICommandHandler<UpdateTemplateCommand>
{
    public async ValueTask<Result> Handle(UpdateTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = renderer.Declared.FirstOrDefault(t => t.Key == command.Key);
        if (template is null || !template.Languages.ContainsKey(command.Language))
        {
            return Error.NotFound("notifications.template_not_found");
        }

        var current = await overrides.FirstOrDefaultAsync(
            new Specification<NotificationTemplateOverride>().Where(o => o.Key == command.Key && o.Language == command.Language).Tracked(),
            cancellationToken);
        var reset = string.IsNullOrWhiteSpace(command.Subject) && string.IsNullOrWhiteSpace(command.Body);

        if (reset)
        {
            if (current is not null)
            {
                overrides.Remove(current);
            }
        }
        else if (current is null)
        {
            overrides.Add(new NotificationTemplateOverride { Key = command.Key, Language = command.Language, Subject = command.Subject ?? string.Empty, Body = command.Body ?? string.Empty });
        }
        else
        {
            current.Subject = command.Subject ?? string.Empty;
            current.Body = command.Body ?? string.Empty;
        }

        await overrides.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Templates of the Notifications module itself.</summary>
public static class NotificationTemplates
{
    public const string Test = "notifications.test";

    public static NotificationTemplate TestTemplate { get; } = new(
        Test,
        NotificationsPermissions.Module,
        new Dictionary<string, LocalizedTemplate>
        {
            ["es"] = new("Notificación de prueba", "Hola {{user}}, las notificaciones y el correo están funcionando."),
            ["en"] = new("Test notification", "Hello {{user}}, notifications and email are working."),
        },
        ["user"]);
}

public sealed class NotificationsPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(NotificationsPermissions.Templates.Manage, NotificationsPermissions.Module, "permissions.notifications.templates.manage", "templates"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("notifications.templates", NotificationsPermissions.Module, "nav.notificationTemplates", "/notifications/templates", "mail", NotificationsPermissions.Templates.Manage, 620),
    ];
}
