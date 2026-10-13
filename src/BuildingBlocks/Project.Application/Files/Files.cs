using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions;
using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Application.Files;

/// <summary>A file to store. <see cref="EntityType"/> and <see cref="EntityId"/> attach it to a business record.</summary>
public sealed record FileUpload(string FileName, string ContentType, long Length, Stream Content, string? EntityType = null, string? EntityId = null);

public sealed record FileDescriptor(
    Guid Id,
    string FileName,
    string ContentType,
    long Length,
    string Provider,
    string? EntityType,
    string? EntityId,
    DateTimeOffset CreatedAt,
    string? CreatedBy);

/// <summary>An opened file. The caller disposes <see cref="Content"/>.</summary>
public sealed record FileContent(FileDescriptor File, Stream Content);

/// <summary>
/// Files API for the other modules (store a generated invoice, read an uploaded resume). The storage provider
/// (local disk, S3, Azure Blob, Google Drive, OneDrive) is chosen per tenant; callers never know which one.
/// When the Files module is not loaded every call fails with <c>files.disabled</c>.
/// </summary>
public interface IFileService
{
    Task<Result<FileDescriptor>> UploadAsync(FileUpload upload, CancellationToken ct);

    Task<Result<FileContent>> OpenAsync(Guid id, CancellationToken ct);

    Task<Result> DeleteAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<FileDescriptor>> ListAsync(string entityType, string entityId, CancellationToken ct);
}

/// <summary>
/// A kind of record that accepts attachments, declared by the module that owns it. Attachments inherit its
/// permissions, and <see cref="ExistsAsync"/> runs with the global filters (tenant, organization unit), so a user
/// can only attach files to, or read files of, records he can see.
/// </summary>
public sealed record AttachmentTarget(
    string EntityType,
    string ReadPermission,
    string WritePermission,
    Func<IServiceProvider, string, CancellationToken, Task<bool>> ExistsAsync);

public static class AttachmentTargetExtensions
{
    /// <summary>
    /// Lets users attach files to <typeparamref name="TEntity"/>:
    /// <c>services.AddAttachmentTarget&lt;Product&gt;("catalog.product", CatalogPermissions.Products.Read, "catalog.products.update")</c>.
    /// </summary>
    public static IServiceCollection AddAttachmentTarget<TEntity>(
        this IServiceCollection services,
        string entityType,
        string readPermission,
        string writePermission)
        where TEntity : AggregateRoot<Guid> =>
        services.AddSingleton(new AttachmentTarget(entityType, readPermission, writePermission, async (sp, id, ct) =>
            Guid.TryParse(id, out var key)
            && await sp.GetRequiredService<IRepository<TEntity, Guid>>().AnyAsync(new Specification<TEntity>().Where(e => e.Id == key), ct)));
}
