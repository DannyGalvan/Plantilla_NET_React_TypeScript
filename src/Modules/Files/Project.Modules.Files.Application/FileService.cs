using System.Security.Cryptography;
using Project.Application.Abstractions;
using Project.Application.Files;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Modules.Files.Domain;
using Project.SharedKernel;

namespace Project.Modules.Files.Application;

/// <summary>
/// Stores files in the storage of the current tenant and keeps their metadata. Callers (endpoints or other modules)
/// are responsible for authorization; see <see cref="FileAuthorization"/>.
/// </summary>
public sealed class FileService(
    IRepository<FileObject, Guid> files,
    IStorageAccountResolver accounts,
    IEnumerable<IFileStorage> storages,
    ISettingsProvider settings,
    ITenantContext tenant,
    TimeProvider clock) : IFileService
{
    private const int MaxFileNameLength = 255;

    public async Task<Result<FileDescriptor>> UploadAsync(FileUpload upload, CancellationToken ct)
    {
        var fileName = Path.GetFileName(upload.FileName ?? string.Empty).Trim();
        if (fileName.Length is 0 or > MaxFileNameLength)
        {
            return Error.Validation("file", "The file name is empty or too long.");
        }

        var limits = await settings.GetAsync<FileSettings>(ct);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!limits.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Error.Validation("file", $"Files of type '{extension}' are not allowed.");
        }

        var maxBytes = (long)limits.MaxFileSizeMb * 1024 * 1024;
        if (upload.Length <= 0 || upload.Length > maxBytes)
        {
            return Error.Validation("file", $"The file must be between 1 byte and {limits.MaxFileSizeMb} MB.");
        }

        var account = await accounts.ResolveDefaultAsync(ct);
        var storage = Storage(account.Provider);
        var file = new FileObject
        {
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(upload.ContentType) ? "application/octet-stream" : upload.ContentType,
            Length = upload.Length,
            Provider = account.Provider,
            ConnectionId = account.ConnectionId,
            EntityType = upload.EntityType,
            EntityId = upload.EntityId,
        };

        var stored = new StoredObject(file.Id, tenant.TenantId ?? TenantDefaults.DefaultTenantId, fileName, file.ContentType, upload.Length, clock.GetUtcNow());
        await using var hashing = new HashingStream(upload.Content, maxBytes);
        file.Location = await storage.SaveAsync(account, stored, hashing, ct);
        file.Sha256 = hashing.Hash;

        try
        {
            files.Add(file);
            await files.UnitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            // Keep storage and database consistent: the object without metadata would be unreachable.
            await storage.DeleteAsync(account, file.Location, CancellationToken.None);
            throw;
        }

        return ToDescriptor(file);
    }

    public async Task<Result<FileContent>> OpenAsync(Guid id, CancellationToken ct)
    {
        var file = await files.GetByIdAsync(id, ct);
        if (file is null)
        {
            return Error.NotFound("files.not_found");
        }

        var account = await accounts.ResolveAsync(file.Provider, file.ConnectionId, ct);
        if (account is null)
        {
            return Error.Conflict("files.storage_disconnected", "The storage that holds this file is no longer connected.");
        }

        var content = await Storage(file.Provider).OpenReadAsync(account, file.Location, ct);
        return new FileContent(ToDescriptor(file), content);
    }

    /// <summary>Soft delete: the metadata disappears now and the object is purged later (restorable meanwhile).</summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var file = await files.GetByIdAsync(id, ct);
        if (file is null)
        {
            return Error.NotFound("files.not_found");
        }

        files.Remove(file);
        await files.UnitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<FileDescriptor>> ListAsync(string entityType, string entityId, CancellationToken ct) =>
        (await files.ListAsync(
            new Specification<FileObject>().Where(f => f.EntityType == entityType && f.EntityId == entityId).OrderBy(f => f.CreatedAt, descending: true),
            ct))
        .Select(ToDescriptor)
        .ToList();

    internal static FileDescriptor ToDescriptor(FileObject file) =>
        new(file.Id, file.FileName, file.ContentType, file.Length, file.Provider, file.EntityType, file.EntityId, file.CreatedAt, file.CreatedBy);

    private IFileStorage Storage(string provider) =>
        storages.FirstOrDefault(s => s.Provider == provider)
        ?? throw new InvalidOperationException($"The storage provider '{provider}' is not registered.");
}

/// <summary>Read-only pass-through stream that computes SHA-256 and enforces a size limit while the provider reads.</summary>
internal sealed class HashingStream(Stream inner, long maxBytes) : Stream
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private long _read;

    public string Hash => Convert.ToHexStringLower(_hash.GetCurrentHash());

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _read;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Track(buffer.AsSpan(offset, inner.Read(buffer, offset, count)));

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);
        return Track(buffer.Span[..read]);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hash.Dispose();
        }

        base.Dispose(disposing);
    }

    private int Track(ReadOnlySpan<byte> chunk)
    {
        _read += chunk.Length;
        if (_read > maxBytes)
        {
            throw new InvalidDataException("The file is larger than the allowed size.");
        }

        _hash.AppendData(chunk);
        return chunk.Length;
    }
}
