using Project.SharedKernel.Domain;

namespace Project.Modules.Files.Domain;

/// <summary>
/// Metadata of a stored file. The bytes live in the storage provider of the tenant (<see cref="Provider"/>,
/// <see cref="ConnectionId"/>) under <see cref="Location"/>; the database only keeps the reference.
/// </summary>
public sealed class FileObject : AuditableAggregateRoot, IMultiTenant
{
    public Guid TenantId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public long Length { get; set; }

    /// <summary>Hex SHA-256 of the content, to detect corruption and duplicates.</summary>
    public string Sha256 { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    /// <summary>Storage connection of the tenant; null for the platform default storage.</summary>
    public Guid? ConnectionId { get; set; }

    /// <summary>Provider key: object key, blob name or Drive/OneDrive item id.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Kind of record the file is attached to (for example <c>catalog.product</c>); null for library files.</summary>
    public string? EntityType { get; set; }

    public string? EntityId { get; set; }
}
