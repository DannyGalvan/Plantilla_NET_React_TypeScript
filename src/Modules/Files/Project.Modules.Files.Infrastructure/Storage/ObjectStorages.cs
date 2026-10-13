using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Project.Modules.Files.Application;
using Project.Modules.Files.Contracts;

namespace Project.Modules.Files.Infrastructure.Storage;

/// <summary>Local disk (or a mounted volume). Platform storage only: tenants cannot choose server paths.</summary>
public sealed class LocalFileStorage(IHostEnvironment environment) : IFileStorage
{
    public string Provider => StorageProviders.Local;

    public bool AllowsTenantConnections => false;

    public string? Validate(IReadOnlyDictionary<string, string> settings, IReadOnlyDictionary<string, string> credentials) =>
        "Local storage is configured by the platform.";

    public async Task<string> SaveAsync(StorageAccount account, StoredObject file, Stream content, CancellationToken ct)
    {
        var path = Resolve(account, file.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(target, ct);
        return file.Key;
    }

    public Task<Stream> OpenReadAsync(StorageAccount account, string location, CancellationToken ct) =>
        Task.FromResult<Stream>(new FileStream(Resolve(account, location), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));

    public Task DeleteAsync(StorageAccount account, string location, CancellationToken ct)
    {
        File.Delete(Resolve(account, location));
        return Task.CompletedTask;
    }

    private string Resolve(StorageAccount account, string location)
    {
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, account.Setting("rootPath") ?? Path.Combine("App_Data", "files")));
        var path = Path.GetFullPath(Path.Combine(root, location));
        return path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? path
            : throw new InvalidOperationException("The file location is outside of the storage root.");
    }
}

/// <summary>Amazon S3 and compatible services (MinIO, Cloudflare R2, Wasabi when custom endpoints are allowed).</summary>
public sealed class S3FileStorage(IOptionsMonitor<FilesOptions> options) : IFileStorage
{
    public string Provider => StorageProviders.S3;

    public bool AllowsTenantConnections => true;

    public string? Validate(IReadOnlyDictionary<string, string> settings, IReadOnlyDictionary<string, string> credentials)
    {
        if (!settings.TryGetValue("bucket", out var bucket) || string.IsNullOrWhiteSpace(bucket))
        {
            return "The bucket is required.";
        }

        if (!credentials.ContainsKey("accessKeyId") || !credentials.ContainsKey("secretAccessKey"))
        {
            return "The access key id and the secret access key are required.";
        }

        if (settings.TryGetValue("serviceUrl", out var url) && !string.IsNullOrWhiteSpace(url))
        {
            return options.CurrentValue.AllowCustomEndpoints && Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http"
                ? null
                : "Custom endpoints are not allowed on this platform.";
        }

        return settings.TryGetValue("region", out var region) && !string.IsNullOrWhiteSpace(region) ? null : "The region is required.";
    }

    public async Task<string> SaveAsync(StorageAccount account, StoredObject file, Stream content, CancellationToken ct)
    {
        using var client = Client(account);
        var request = new PutObjectRequest
        {
            BucketName = account.Setting("bucket"),
            Key = Prefixed(account, file.Key),
            InputStream = content,
            ContentType = file.ContentType,
            AutoCloseStream = false,
        };
        request.Headers.ContentLength = file.Length;
        await client.PutObjectAsync(request, ct);
        return request.Key;
    }

    public async Task<Stream> OpenReadAsync(StorageAccount account, string location, CancellationToken ct)
    {
        var client = Client(account);
        var response = await client.GetObjectAsync(account.Setting("bucket"), location, ct);
        return new OwnedStream(response.ResponseStream, response, client);
    }

    public async Task DeleteAsync(StorageAccount account, string location, CancellationToken ct)
    {
        using var client = Client(account);
        await client.DeleteObjectAsync(account.Setting("bucket"), location, ct);
    }

    private static string Prefixed(StorageAccount account, string key) =>
        account.Setting("prefix") is { } prefix ? $"{prefix.Trim('/')}/{key}" : key;

    private static AmazonS3Client Client(StorageAccount account)
    {
        var credentials = new BasicAWSCredentials(account.Credential("accessKeyId"), account.Credential("secretAccessKey"));
        var config = new AmazonS3Config();
        if (account.Setting("serviceUrl") is { } url)
        {
            config.ServiceURL = url;
            config.ForcePathStyle = true;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(account.Setting("region"));
        }

        return new AmazonS3Client(credentials, config);
    }
}

/// <summary>Azure Blob Storage (or Azurite when custom endpoints are allowed).</summary>
public sealed class AzureBlobFileStorage(IOptionsMonitor<FilesOptions> options) : IFileStorage
{
    public string Provider => StorageProviders.AzureBlob;

    public bool AllowsTenantConnections => true;

    public string? Validate(IReadOnlyDictionary<string, string> settings, IReadOnlyDictionary<string, string> credentials)
    {
        if (!settings.TryGetValue("container", out var container) || string.IsNullOrWhiteSpace(container))
        {
            return "The container is required.";
        }

        if (!credentials.TryGetValue("connectionString", out var connectionString) || string.IsNullOrWhiteSpace(connectionString))
        {
            return "The connection string is required.";
        }

        try
        {
            var host = new BlobServiceClient(connectionString).Uri.Host;
            return options.CurrentValue.AllowCustomEndpoints || host.EndsWith(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase)
                ? null
                : "Custom endpoints are not allowed on this platform.";
        }
        catch (FormatException)
        {
            return "The connection string is not valid.";
        }
    }

    public async Task<string> SaveAsync(StorageAccount account, StoredObject file, Stream content, CancellationToken ct)
    {
        var blob = Container(account).GetBlobClient(file.Key);
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } }, ct);
        return file.Key;
    }

    public async Task<Stream> OpenReadAsync(StorageAccount account, string location, CancellationToken ct) =>
        (await Container(account).GetBlobClient(location).DownloadStreamingAsync(cancellationToken: ct)).Value.Content;

    public Task DeleteAsync(StorageAccount account, string location, CancellationToken ct) =>
        Container(account).GetBlobClient(location).DeleteIfExistsAsync(cancellationToken: ct);

    private static BlobContainerClient Container(StorageAccount account) =>
        new(account.Credential("connectionString"), account.Setting("container"));
}

/// <summary>Response stream that also disposes the objects that own it (HTTP response, SDK client).</summary>
internal sealed class OwnedStream(Stream inner, params IDisposable[] owners) : Stream
{
    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        inner.ReadAsync(buffer, offset, count, cancellationToken);

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
            inner.Dispose();
            foreach (var owner in owners)
            {
                owner.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
