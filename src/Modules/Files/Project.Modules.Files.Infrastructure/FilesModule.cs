using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application;
using Project.Application.Configuration;
using Project.Application.Files;
using Project.Application.Jobs;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Files.Application;
using Project.Modules.Files.Domain;
using Project.Modules.Files.Infrastructure.Storage;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Files.Infrastructure;

public sealed class FilesDbContext(DbContextOptions<FilesDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => "files";

    public DbSet<FileObject> Files => Set<FileObject>();

    public DbSet<StorageConnection> StorageConnections => Set<StorageConnection>();
}

internal sealed class FileObjectConfiguration : IEntityTypeConfiguration<FileObject>
{
    public void Configure(EntityTypeBuilder<FileObject> builder)
    {
        builder.ToTable("Files");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FileName).HasMaxLength(255).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(f => f.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(f => f.Provider).HasMaxLength(32).IsRequired();
        builder.Property(f => f.Location).HasMaxLength(1024).IsRequired();
        builder.Property(f => f.EntityType).HasMaxLength(64);
        builder.Property(f => f.EntityId).HasMaxLength(64);
        builder.Property(f => f.CreatedBy).HasMaxLength(64);
        builder.Property(f => f.UpdatedBy).HasMaxLength(64);
        builder.Property(f => f.DeletedBy).HasMaxLength(64);
        builder.HasIndex(f => new { f.TenantId, f.EntityType, f.EntityId });
        builder.HasIndex(f => new { f.TenantId, f.CreatedAt });
    }
}

internal sealed class StorageConnectionConfiguration : IEntityTypeConfiguration<StorageConnection>
{
    public void Configure(EntityTypeBuilder<StorageConnection> builder)
    {
        builder.ToTable("StorageConnections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(128).IsRequired();
        builder.Property(c => c.Provider).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Settings).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.ProtectedCredentials).IsRequired();
        builder.Property(c => c.Account).HasMaxLength(256);
        builder.Property(c => c.CreatedBy).HasMaxLength(64);
        builder.Property(c => c.UpdatedBy).HasMaxLength(64);
        builder.Property(c => c.DeletedBy).HasMaxLength(64);
        builder.HasIndex(c => new { c.TenantId, c.IsDefault });
    }
}

/// <summary>Removes the content of files deleted more than <c>files.PurgeDeletedAfterDays</c> ago, then their metadata.</summary>
public sealed class PurgeDeletedFilesJob(
    FilesDbContext db,
    IStorageAccountResolver accounts,
    IEnumerable<IFileStorage> storages,
    ISettingsProvider settings,
    TimeProvider clock,
    ILogger<PurgeDeletedFilesJob> logger) : IRecurringJob
{
    public const string Name = "files.purge-deleted";

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var days = (await settings.GetAsync<FileSettings>(ct)).PurgeDeletedAfterDays;
        if (days <= 0)
        {
            return;
        }

        var cutoff = clock.GetUtcNow().AddDays(-days);
        while (true)
        {
            var batch = await db.Files.IgnoreQueryFilters([QueryFilters.SoftDelete]).AsNoTracking()
                .Where(f => f.IsDeleted && f.DeletedAt < cutoff)
                .OrderBy(f => f.DeletedAt)
                .Take(100)
                .ToListAsync(ct);

            foreach (var file in batch)
            {
                try
                {
                    if (await accounts.ResolveAsync(file.Provider, file.ConnectionId, ct) is { } account
                        && storages.FirstOrDefault(s => s.Provider == file.Provider) is { } storage)
                    {
                        await storage.DeleteAsync(account, file.Location, ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "The content of file {FileId} could not be removed from {Provider}", file.Id, file.Provider);
                }
            }

            var ids = batch.Select(f => f.Id).ToList();
            await db.Files.IgnoreQueryFilters([QueryFilters.SoftDelete]).Where(f => ids.Contains(f.Id)).ExecuteDeleteAsync(ct);
            if (batch.Count < 100)
            {
                return;
            }
        }
    }
}

public sealed record StartOAuthRequest(string Name, bool IsDefault);

public sealed record CreateConnectionRequest(string Name, string Provider, Dictionary<string, string>? Settings, Dictionary<string, string>? Credentials, bool IsDefault);

public sealed record SetDefaultConnectionRequest(Guid? ConnectionId);

/// <summary>
/// Files and attachments for every module, stored on the provider chosen by each tenant: platform storage (local
/// disk, S3 or Azure Blob) or the tenant's own Google Drive, OneDrive, S3 bucket or Azure container.
/// </summary>
public sealed class FilesModule : IModule
{
    public string Name => "Files";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<FilesDbContext>(Name, DataScope);
        services.AddRepositoriesFor<FilesDbContext>();
        services.AddModuleConventions(typeof(FilesPermissionProvider).Assembly);
        services.AddSettingsSection<FileSettings>();
        services.AddOptions<FilesOptions>().Bind(configuration.GetSection(FilesOptions.SectionName));
        services.AddConfigurableSection<FilesOptions>(FilesOptions.SectionName, "files", "Almacenamiento de la plataforma y clientes OAuth de Google Drive y OneDrive.");

        services.AddScoped<IFileService, FileService>();
        services.AddScoped<FileAuthorization>();
        services.AddScoped<OAuthStateReader>();
        services.AddScoped<IStorageAccountResolver, StorageAccountResolver>();
        services.AddRecurringJob<PurgeDeletedFilesJob>(PurgeDeletedFilesJob.Name, "30 3 * * *", description: "Removes deleted files after the retention period.");

        // Storage providers (strategies). A new provider is one more registration.
        services.AddMemoryCache();
        services.AddHttpClient(CloudDriveStorage.HttpClientName);
        services.TryAddSingleton<GoogleDriveStorage>();
        services.TryAddSingleton<OneDriveStorage>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileStorage, LocalFileStorage>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileStorage, S3FileStorage>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileStorage, AzureBlobFileStorage>());
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<GoogleDriveStorage>());
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<OneDriveStorage>());
        services.AddSingleton<IOAuthStorageProvider>(sp => sp.GetRequiredService<GoogleDriveStorage>());
        services.AddSingleton<IOAuthStorageProvider>(sp => sp.GetRequiredService<OneDriveStorage>());
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var maxRequest = api.ServiceProvider.GetRequiredService<IOptions<FilesOptions>>().Value.MaxRequestSizeMb * 1024L * 1024L;
        var files = api.MapGroup("files").WithTags("files");

        // Authorization depends on the record the file belongs to, so it is enforced by the handlers.
        files.MapGet("/", async ([AsParameters] ListFilesQuery query, ISender sender, CancellationToken ct) => (await sender.Send(query, ct)).ToHttp())
            .RequireAuthenticatedUser()
            .WithName("ListFiles");

        files.MapPost("/", async (IFormFile file, [FromForm] string? entityType, [FromForm] string? entityId, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                var upload = new FileUpload(file.FileName, ContentTypeOf(file.FileName), file.Length, content, Blank(entityType), Blank(entityId));
                return (await sender.Send(new UploadFileCommand(upload), ct)).ToCreated(f => $"{http.Request.Path.ToString().TrimEnd('/')}/{f.Id}");
            })
            .RequireAuthenticatedUser()
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(maxRequest), new RequestFormLimitsAttribute { MultipartBodyLengthLimit = maxRequest })
            .WithName("UploadFile");

        files.MapGet("/{id:guid}/content", async (Guid id, bool? inline, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DownloadFileQuery(id), ct);
                if (result.IsFailure)
                {
                    return result.ToHttp();
                }

                var file = result.Value.File;
                var showInline = inline == true && InlineTypes.Contains(file.ContentType);
                return Results.Stream(result.Value.Content, file.ContentType, showInline ? null : file.FileName);
            })
            .RequireAuthenticatedUser()
            .WithName("DownloadFile");

        files.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => (await sender.Send(new DeleteFileCommand(id), ct)).ToHttp())
            .RequireAuthenticatedUser()
            .WithName("DeleteFile");

        var storage = files.MapGroup("/storage").WithTags("files-storage");
        storage.MapGet("/providers", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListStorageProvidersQuery(), ct)).ToHttp())
            .RequirePermission(Contracts.FilesPermissions.Storage.Manage)
            .WithName("ListStorageProviders");
        storage.MapGet("/connections", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListConnectionsQuery(), ct)).ToHttp())
            .RequirePermission(Contracts.FilesPermissions.Storage.Manage)
            .WithName("ListStorageConnections");
        storage.MapPost("/connections", async (CreateConnectionRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CreateConnectionCommand(request.Name, request.Provider, request.Settings, request.Credentials, request.IsDefault), ct)).ToHttp())
            .RequirePermission(Contracts.FilesPermissions.Storage.Manage)
            .WithName("CreateStorageConnection");
        storage.MapPut("/default", async (SetDefaultConnectionRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetDefaultConnectionCommand(request.ConnectionId), ct)).ToHttp())
            .RequirePermission(Contracts.FilesPermissions.Storage.Manage)
            .WithName("SetDefaultStorageConnection");
        storage.MapDelete("/connections/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteConnectionCommand(id), ct)).ToHttp())
            .RequirePermission(Contracts.FilesPermissions.Storage.Manage)
            .WithName("DeleteStorageConnection");

        storage.MapPost("/oauth/{provider}/start", async (string provider, StartOAuthRequest request, IOptionsSnapshot<FilesOptions> options, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var command = new StartOAuthCommand(provider, request.Name, request.IsDefault, RedirectUri(options.Value, http), ReturnUrl(http));
                var result = await sender.Send(command, ct);
                return result.IsFailure ? result.ToHttp() : Results.Ok(new { url = result.Value });
            })
            .RequirePermission(Contracts.FilesPermissions.Storage.Manage)
            .WithName("StartStorageOAuth");

        // Google and Microsoft redirect the browser here; the encrypted state carries the tenant and the user.
        files.MapGet("/oauth/callback", async (string? code, string? state, string? error, OAuthStateReader reader, ITenantJobRunner runner, ILoggerFactory logs, CancellationToken ct) =>
            {
                var parsed = reader.Read(state);
                if (parsed is null)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "The authorization request expired or is not valid.");
                }

                var connected = false;
                if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(code))
                {
                    try
                    {
                        await runner.RunForTenantAsync(parsed.TenantId, async (services, _, token) =>
                            connected = (await services.GetRequiredService<ISender>().Send(new CompleteOAuthCommand(parsed, code), token)).IsSuccess, ct);
                    }
                    catch (HttpRequestException ex)
                    {
                        logs.CreateLogger<FilesModule>().LogWarning(ex, "The {Provider} authorization could not be completed", parsed.Provider);
                    }
                }

                return Results.Redirect($"{parsed.ReturnUrl}?storage={(connected ? "connected" : "failed")}");
            })
            .AllowAnonymous()
            .AllowWithoutTenant()
            .ExcludeFromDescription();
    }

    private static readonly HashSet<string> InlineTypes = ["image/png", "image/jpeg", "image/gif", "image/webp", "application/pdf", "text/plain"];

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private static string ContentTypeOf(string fileName) =>
        ContentTypes.TryGetContentType(fileName, out var type) ? type : "application/octet-stream";

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string RedirectUri(FilesOptions options, HttpContext http) =>
        options.OAuth.RedirectUri is { Length: > 0 } configured
            ? configured
            : $"{http.Request.Scheme}://{http.Request.Host}{ModuleLoader.ApiPrefix}/files/oauth/callback";

    /// <summary>Page of the SPA that started the flow (the origin of the browser, or this host).</summary>
    private static string ReturnUrl(HttpContext http)
    {
        var origin = http.Request.Headers.Origin.ToString();
        var site = Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http"
            ? uri.GetLeftPart(UriPartial.Authority)
            : $"{http.Request.Scheme}://{http.Request.Host}";
        return $"{site}/files/storage";
    }
}
