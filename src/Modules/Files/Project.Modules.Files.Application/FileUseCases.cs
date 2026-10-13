using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Files;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Modules.Files.Contracts;
using Project.Modules.Files.Domain;
using Project.SharedKernel;

namespace Project.Modules.Files.Application;

public enum FileOperation
{
    Read,
    Write,
}

/// <summary>
/// Decides who may touch a file. Library files use <c>files.library.*</c>; attachments use the permissions of the
/// record they belong to and require that record to be visible to the caller (tenant and organization filters), so
/// a guessed id never leaks a file of another tenant or unit.
/// </summary>
public sealed class FileAuthorization(IEnumerable<AttachmentTarget> targets, IPermissionService permissions, IServiceProvider services)
{
    public IReadOnlyCollection<string> EntityTypes => targets.Select(t => t.EntityType).ToList();

    public async Task<Error?> AuthorizeAsync(string? entityType, string? entityId, FileOperation operation, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            var permission = operation == FileOperation.Read ? FilesPermissions.Library.Read : FilesPermissions.Library.Upload;
            return await permissions.HasPermissionAsync(permission, ct) ? null : Error.Forbidden("auth.forbidden");
        }

        var target = targets.FirstOrDefault(t => t.EntityType == entityType);
        if (target is null)
        {
            return Error.Validation("entityType", "Files cannot be attached to this kind of record.");
        }

        var required = operation == FileOperation.Read ? target.ReadPermission : target.WritePermission;
        if (!await permissions.HasPermissionAsync(required, ct))
        {
            return Error.Forbidden("auth.forbidden");
        }

        return !string.IsNullOrWhiteSpace(entityId) && await target.ExistsAsync(services, entityId, ct)
            ? null
            : Error.NotFound("files.record_not_found", "The record does not exist.");
    }

    /// <summary>Deleting a library file needs its own permission; an attachment needs write access to the record.</summary>
    public async Task<Error?> AuthorizeDeleteAsync(FileObject file, CancellationToken ct) =>
        file.EntityType is null
            ? await permissions.HasPermissionAsync(FilesPermissions.Library.Delete, ct) ? null : Error.Forbidden("auth.forbidden")
            : await AuthorizeAsync(file.EntityType, file.EntityId, FileOperation.Write, ct);
}

public sealed record UploadFileCommand(FileUpload Upload) : ICommand<FileDescriptor>;

public sealed class UploadFileHandler(FileAuthorization access, IFileService files) : ICommandHandler<UploadFileCommand, FileDescriptor>
{
    public async ValueTask<Result<FileDescriptor>> Handle(UploadFileCommand command, CancellationToken cancellationToken)
    {
        var denied = await access.AuthorizeAsync(command.Upload.EntityType, command.Upload.EntityId, FileOperation.Write, cancellationToken);
        return denied is not null ? denied : await files.UploadAsync(command.Upload, cancellationToken);
    }
}

public sealed record ListFilesQuery(string? EntityType, string? EntityId, string? Search, int Page = 1, int PageSize = 50) : IQuery<PagedResult<FileDescriptor>>;

public sealed class ListFilesHandler(FileAuthorization access, IRepository<FileObject, Guid> files) : IQueryHandler<ListFilesQuery, PagedResult<FileDescriptor>>
{
    public async ValueTask<Result<PagedResult<FileDescriptor>>> Handle(ListFilesQuery query, CancellationToken cancellationToken)
    {
        var entityType = string.IsNullOrWhiteSpace(query.EntityType) ? null : query.EntityType;
        var denied = await access.AuthorizeAsync(entityType, query.EntityId, FileOperation.Read, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 200);
        var entityId = entityType is null ? null : query.EntityId;
        var spec = new Specification<FileObject>().Where(f => f.EntityType == entityType && f.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            spec.Where(f => f.FileName.Contains(term));
        }

        var total = await files.CountAsync(spec, cancellationToken);
        var items = await files.ListAsync(spec.OrderBy(f => f.CreatedAt, descending: true).Page((page - 1) * size, size), cancellationToken);
        return new PagedResult<FileDescriptor>(items.Select(FileService.ToDescriptor).ToList(), page, size, total);
    }
}

public sealed record DownloadFileQuery(Guid Id) : IQuery<FileContent>;

public sealed class DownloadFileHandler(FileAuthorization access, IRepository<FileObject, Guid> files, IFileService service)
    : IQueryHandler<DownloadFileQuery, FileContent>
{
    public async ValueTask<Result<FileContent>> Handle(DownloadFileQuery query, CancellationToken cancellationToken)
    {
        var file = await files.GetByIdAsync(query.Id, cancellationToken);
        if (file is null)
        {
            return Error.NotFound("files.not_found");
        }

        // A forbidden attachment is reported as not found: the caller does not learn that the id exists.
        var denied = await access.AuthorizeAsync(file.EntityType, file.EntityId, FileOperation.Read, cancellationToken);
        return denied is null ? await service.OpenAsync(file.Id, cancellationToken) : Error.NotFound("files.not_found");
    }
}

public sealed record DeleteFileCommand(Guid Id) : ICommand;

public sealed class DeleteFileHandler(FileAuthorization access, IRepository<FileObject, Guid> files, IFileService service) : ICommandHandler<DeleteFileCommand>
{
    public async ValueTask<Result> Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        var file = await files.GetByIdAsync(command.Id, cancellationToken);
        if (file is null)
        {
            return Error.NotFound("files.not_found");
        }

        var denied = await access.AuthorizeDeleteAsync(file, cancellationToken);
        if (denied is not null)
        {
            return denied.Type == ErrorType.Forbidden ? denied : Error.NotFound("files.not_found");
        }

        return await service.DeleteAsync(file.Id, cancellationToken);
    }
}

/// <summary>Permissions and navigation of the Files module.</summary>
public sealed class FilesPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(FilesPermissions.Library.Read, FilesPermissions.Module, "permissions.files.library.read", "library"),
        new(FilesPermissions.Library.Upload, FilesPermissions.Module, "permissions.files.library.upload", "library"),
        new(FilesPermissions.Library.Delete, FilesPermissions.Module, "permissions.files.library.delete", "library"),
        new(FilesPermissions.Storage.Manage, FilesPermissions.Module, "permissions.files.storage.manage", "storage"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("files.library", FilesPermissions.Module, "nav.files", "/files", "folder-open", FilesPermissions.Library.Read, 600),
        new("files.storage", FilesPermissions.Module, "nav.storage", "/files/storage", "hard-drive", FilesPermissions.Storage.Manage, 610),
    ];
}
