using System.Text.Json;
using FluentValidation;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Application.Tenancy;
using Project.Modules.Files.Contracts;
using Project.Modules.Files.Domain;
using Project.SharedKernel;

namespace Project.Modules.Files.Application;

/// <summary>A connection as administrators see it: never includes credentials.</summary>
public sealed record StorageConnectionDto(
    Guid Id,
    string Name,
    string Provider,
    string? Account,
    bool IsDefault,
    IReadOnlyDictionary<string, string> Settings,
    DateTimeOffset CreatedAt)
{
    public static StorageConnectionDto From(StorageConnection connection) =>
        new(
            connection.Id,
            connection.Name,
            connection.Provider,
            connection.Account,
            connection.IsDefault,
            JsonSerializer.Deserialize<Dictionary<string, string>>(connection.Settings) ?? [],
            connection.CreatedAt);
}

/// <summary>Providers the tenant can connect and how (credentials form or OAuth consent).</summary>
public sealed record StorageProviderDto(string Provider, bool UsesOAuth, bool Available);

internal static class ConnectionSecrets
{
    public const string Purpose = "files.storage-credentials";
    public const string StatePurpose = "files.oauth-state";

    public static string Protect(ISecretProtector protector, IReadOnlyDictionary<string, string> credentials) =>
        protector.Protect(Purpose, JsonSerializer.Serialize(credentials));
}

public sealed record ListStorageProvidersQuery : IQuery<IReadOnlyList<StorageProviderDto>>, IRequirePermission
{
    public string Permission => FilesPermissions.Storage.Manage;
}

public sealed class ListStorageProvidersHandler(IEnumerable<IFileStorage> storages, IEnumerable<IOAuthStorageProvider> oauth)
    : IQueryHandler<ListStorageProvidersQuery, IReadOnlyList<StorageProviderDto>>
{
    public ValueTask<Result<IReadOnlyList<StorageProviderDto>>> Handle(ListStorageProvidersQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<StorageProviderDto> providers = storages
            .Where(s => s.AllowsTenantConnections)
            .Select(s => oauth.FirstOrDefault(o => o.Provider == s.Provider) is { } flow
                ? new StorageProviderDto(s.Provider, UsesOAuth: true, flow.IsConfigured)
                : new StorageProviderDto(s.Provider, UsesOAuth: false, Available: true))
            .OrderBy(p => p.Provider, StringComparer.Ordinal)
            .ToList();
        return ValueTask.FromResult(Result<IReadOnlyList<StorageProviderDto>>.Success(providers));
    }
}

public sealed record ListConnectionsQuery : IQuery<IReadOnlyList<StorageConnectionDto>>, IRequirePermission
{
    public string Permission => FilesPermissions.Storage.Manage;
}

public sealed class ListConnectionsHandler(IRepository<StorageConnection, Guid> connections)
    : IQueryHandler<ListConnectionsQuery, IReadOnlyList<StorageConnectionDto>>
{
    public async ValueTask<Result<IReadOnlyList<StorageConnectionDto>>> Handle(ListConnectionsQuery query, CancellationToken cancellationToken)
    {
        var items = await connections.ListAsync(new Specification<StorageConnection>().OrderBy(c => c.Name), cancellationToken);
        return items.Select(StorageConnectionDto.From).ToList();
    }
}

/// <summary>
/// Connects a provider with keys: S3, Azure Blob, a Google service account or a Microsoft app registration.
/// Credentials are encrypted and never returned.
/// </summary>
public sealed record CreateConnectionCommand(
    string Name,
    string Provider,
    Dictionary<string, string>? Settings,
    Dictionary<string, string>? Credentials,
    bool IsDefault) : ICommand<StorageConnectionDto>, IRequirePermission
{
    public string Permission => FilesPermissions.Storage.Manage;
}

public sealed class CreateConnectionValidator : AbstractValidator<CreateConnectionCommand>
{
    public CreateConnectionValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Provider).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Settings).Must(s => s is null || s.Count <= 20).WithMessage("Too many settings.");
    }
}

public sealed class CreateConnectionHandler(
    IRepository<StorageConnection, Guid> connections,
    IEnumerable<IFileStorage> storages,
    ISecretProtector protector) : ICommandHandler<CreateConnectionCommand, StorageConnectionDto>
{
    public async ValueTask<Result<StorageConnectionDto>> Handle(CreateConnectionCommand command, CancellationToken cancellationToken)
    {
        var storage = storages.FirstOrDefault(s => s.Provider == command.Provider && s.AllowsTenantConnections);
        if (storage is null)
        {
            return Error.Validation("provider", "This provider cannot be connected by tenants.");
        }

        var settings = command.Settings ?? [];
        var credentials = command.Credentials ?? [];
        if (storage.Validate(settings, credentials) is { } problem)
        {
            return Error.Validation("credentials", problem);
        }

        var created = StorageConnection.Create(
            command.Name,
            command.Provider,
            JsonSerializer.Serialize(settings),
            ConnectionSecrets.Protect(protector, credentials),
            settings.GetValueOrDefault("bucket") ?? settings.GetValueOrDefault("container"));
        if (created.IsFailure)
        {
            return created.Error!;
        }

        return await ConnectionStore.AddAsync(connections, created.Value, command.IsDefault, cancellationToken);
    }
}

internal static class ConnectionStore
{
    public static async Task<StorageConnectionDto> AddAsync(
        IRepository<StorageConnection, Guid> connections,
        StorageConnection connection,
        bool isDefault,
        CancellationToken ct)
    {
        var first = !await connections.AnyAsync(new Specification<StorageConnection>(), ct);
        if (isDefault || first)
        {
            await ClearDefaultAsync(connections, ct);
            connection.MakeDefault(true);
        }

        connections.Add(connection);
        await connections.UnitOfWork.SaveChangesAsync(ct);
        return StorageConnectionDto.From(connection);
    }

    public static async Task ClearDefaultAsync(IRepository<StorageConnection, Guid> connections, CancellationToken ct)
    {
        foreach (var current in await connections.ListAsync(new Specification<StorageConnection>().Where(c => c.IsDefault).Tracked(), ct))
        {
            current.MakeDefault(false);
        }
    }
}

public sealed record SetDefaultConnectionCommand(Guid? Id) : ICommand, IRequirePermission
{
    public string Permission => FilesPermissions.Storage.Manage;
}

/// <summary>Chooses where new files go. A null id switches back to the platform storage.</summary>
public sealed class SetDefaultConnectionHandler(IRepository<StorageConnection, Guid> connections) : ICommandHandler<SetDefaultConnectionCommand>
{
    public async ValueTask<Result> Handle(SetDefaultConnectionCommand command, CancellationToken cancellationToken)
    {
        StorageConnection? target = null;
        if (command.Id is { } id)
        {
            target = await connections.FirstOrDefaultAsync(new Specification<StorageConnection>().Where(c => c.Id == id).Tracked(), cancellationToken);
            if (target is null)
            {
                return Error.NotFound("files.connection_not_found");
            }
        }

        await ConnectionStore.ClearDefaultAsync(connections, cancellationToken);
        target?.MakeDefault(true);
        await connections.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteConnectionCommand(Guid Id) : ICommand, IRequirePermission
{
    public string Permission => FilesPermissions.Storage.Manage;
}

public sealed class DeleteConnectionHandler(IRepository<StorageConnection, Guid> connections, IRepository<FileObject, Guid> files)
    : ICommandHandler<DeleteConnectionCommand>
{
    public async ValueTask<Result> Handle(DeleteConnectionCommand command, CancellationToken cancellationToken)
    {
        var connection = await connections.FirstOrDefaultAsync(new Specification<StorageConnection>().Where(c => c.Id == command.Id).Tracked(), cancellationToken);
        if (connection is null)
        {
            return Error.NotFound("files.connection_not_found");
        }

        if (await files.AnyAsync(new Specification<FileObject>().Where(f => f.ConnectionId == command.Id), cancellationToken))
        {
            return Error.Conflict("files.connection_in_use", "Files are still stored in this connection. Delete or move them first.");
        }

        connections.Remove(connection);
        await connections.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>State carried through the OAuth consent screen; encrypted and valid for 15 minutes.</summary>
public sealed record OAuthState(Guid TenantId, string UserId, string Provider, string Name, string RedirectUri, string ReturnUrl, bool IsDefault, DateTimeOffset ExpiresAt);

public sealed record StartOAuthCommand(string Provider, string Name, bool IsDefault, string RedirectUri, string ReturnUrl)
    : ICommand<string>, IRequirePermission
{
    public string Permission => FilesPermissions.Storage.Manage;
}

/// <summary>Returns the consent URL of Google or Microsoft for the current tenant administrator.</summary>
public sealed class StartOAuthHandler(
    IEnumerable<IOAuthStorageProvider> providers,
    ISecretProtector protector,
    ITenantContext tenant,
    ICurrentUser user,
    TimeProvider clock) : ICommandHandler<StartOAuthCommand, string>
{
    public ValueTask<Result<string>> Handle(StartOAuthCommand command, CancellationToken cancellationToken)
    {
        var provider = providers.FirstOrDefault(p => p.Provider == command.Provider);
        if (provider is null || !provider.IsConfigured)
        {
            return ValueTask.FromResult<Result<string>>(Error.Validation("provider", "This provider is not configured on the platform."));
        }

        var name = string.IsNullOrWhiteSpace(command.Name) ? command.Provider : command.Name.Trim();
        var state = new OAuthState(
            tenant.TenantId ?? TenantDefaults.DefaultTenantId,
            user.UserId ?? string.Empty,
            provider.Provider,
            name.Length > 128 ? name[..128] : name,
            command.RedirectUri,
            command.ReturnUrl,
            command.IsDefault,
            clock.GetUtcNow().AddMinutes(15));
        var token = protector.Protect(ConnectionSecrets.StatePurpose, JsonSerializer.Serialize(state));
        return ValueTask.FromResult<Result<string>>(provider.BuildAuthorizationUrl(token, command.RedirectUri));
    }
}

/// <summary>Reads and validates the state of an OAuth callback (tampered or expired states are rejected).</summary>
public sealed class OAuthStateReader(ISecretProtector protector, TimeProvider clock)
{
    public OAuthState? Read(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || protector.Unprotect(ConnectionSecrets.StatePurpose, token) is not { } json)
        {
            return null;
        }

        var state = JsonSerializer.Deserialize<OAuthState>(json);
        return state is not null && state.ExpiresAt > clock.GetUtcNow() ? state : null;
    }
}

/// <summary>Runs inside the tenant of the state: exchanges the code and stores the encrypted refresh token.</summary>
public sealed record CompleteOAuthCommand(OAuthState State, string Code) : ICommand<StorageConnectionDto>;

public sealed class CompleteOAuthHandler(
    IEnumerable<IOAuthStorageProvider> providers,
    IRepository<StorageConnection, Guid> connections,
    ISecretProtector protector,
    ITenantContext tenant) : ICommandHandler<CompleteOAuthCommand, StorageConnectionDto>
{
    public async ValueTask<Result<StorageConnectionDto>> Handle(CompleteOAuthCommand command, CancellationToken cancellationToken)
    {
        var state = command.State;
        if ((tenant.TenantId ?? TenantDefaults.DefaultTenantId) != state.TenantId)
        {
            return Error.Forbidden("files.oauth_tenant_mismatch");
        }

        var provider = providers.FirstOrDefault(p => p.Provider == state.Provider);
        if (provider is null || string.IsNullOrWhiteSpace(command.Code))
        {
            return Error.Validation("code", "The authorization was not completed.");
        }

        var grant = await provider.ExchangeCodeAsync(command.Code, state.RedirectUri, cancellationToken);
        var created = StorageConnection.Create(state.Name, state.Provider, "{}", ConnectionSecrets.Protect(protector, grant.Credentials), grant.Account);
        if (created.IsFailure)
        {
            return created.Error!;
        }

        return await ConnectionStore.AddAsync(connections, created.Value, state.IsDefault, cancellationToken);
    }
}
