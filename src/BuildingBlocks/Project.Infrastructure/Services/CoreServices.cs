using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project.Application.Abstractions;
using Project.Application.Files;
using Project.Application.Notifications;
using Project.Application.Organization;
using Project.Application.Security;
using Project.Application.Sequences;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Infrastructure.Tenancy;
using Project.SharedKernel;

namespace Project.Infrastructure.Services;

/// <summary>HybridCache facade. Keys and tags get the <c>t:{tenantId}:</c> prefix whenever a tenant is active.</summary>
public sealed class HybridCacheService(HybridCache cache, ITenantContext tenant) : ICacheService
{
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        IEnumerable<string>? tags = null,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? TimeSpan.FromMinutes(10),
            LocalCacheExpiration = expiration is { } e && e < TimeSpan.FromMinutes(1) ? e : TimeSpan.FromMinutes(1),
        };

        return cache.GetOrCreateAsync(
            Prefix(key),
            factory,
            static (f, token) => f(token),
            options,
            tags?.Select(Prefix),
            ct);
    }

    public ValueTask RemoveAsync(string key, CancellationToken ct = default) => cache.RemoveAsync(Prefix(key), ct);

    public ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default) => cache.RemoveByTagAsync(Prefix(tag), ct);

    private string Prefix(string value) => tenant.TenantId is { } id ? $"t:{id:N}:{value}" : value;
}

/// <summary>Current user from the authenticated <see cref="ClaimsPrincipal"/>; the system account outside HTTP requests.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId => IsAuthenticated ? Principal!.FindFirstValue("sub") ?? Principal!.FindFirstValue(ClaimTypes.NameIdentifier) : null;

    public string? UserName => IsAuthenticated ? Principal!.FindFirstValue("name") ?? Principal!.Identity!.Name : null;

    public Guid? TenantId => Guid.TryParse(Principal?.FindFirstValue(TenantClaims.TenantId), out var id) ? id : null;

    public Guid? ActiveOrganizationUnitId => Guid.TryParse(Principal?.FindFirstValue(TenantClaims.OrganizationUnitId), out var id) ? id : null;

    public long PermissionsVersion => long.TryParse(Principal?.FindFirstValue(TenantClaims.PermissionsVersion), out var pv) ? pv : 0;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Concat(Principal.FindAll("role")).Select(c => c.Value).Distinct().ToArray() ?? [];

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString() is { Length: > 0 } agent
        ? agent[..Math.Min(agent.Length, 512)]
        : null;
}

/// <summary>Used when the Organization module is not registered: no unit scope, no filter.</summary>
public sealed class NoOpOrganizationContext : IOrganizationContext
{
    public bool Enabled => false;

    public Guid? ActiveUnitId => null;

    public bool HasFullAccess => true;

    public IReadOnlySet<Guid> AccessibleUnitIds { get; } = new HashSet<Guid>();

    public IReadOnlyList<Guid> ActiveUnitPath => [];

    public ValueTask InitializeAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

/// <summary>Used when the Organization module is not registered.</summary>
public sealed class NoOpOrganizationDirectory : IOrganizationDirectory
{
    public Task<IReadOnlyList<Guid>> GetPathAsync(Guid unitId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<IReadOnlyList<Guid>> GetAssignedUnitsAsync(string userId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<IReadOnlySet<Guid>> ExpandDescendantsAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlySet<Guid>>(unitIds.ToHashSet());
}

/// <summary>Fallback settings: code defaults overridden by <c>Settings:{SectionKey}</c> in configuration.</summary>
public sealed class ConfigurationSettingsProvider(IConfiguration configuration) : ISettingsProvider
{
    public ValueTask<T> GetAsync<T>(CancellationToken ct)
        where T : class, ISettingsSection, new()
    {
        var section = new T();
        configuration.GetSection($"Settings:{T.SectionKey}").Bind(section);
        return ValueTask.FromResult(section);
    }
}

/// <summary>Fallback feature flags from <c>Features:{name}</c> (boolean) in configuration.</summary>
public sealed class ConfigurationFeatureFlags(IConfiguration configuration) : IFeatureFlags
{
    public ValueTask<bool> IsEnabledAsync(string feature, CancellationToken ct) =>
        ValueTask.FromResult(configuration.GetValue<bool>($"Features:{feature}"));

    public ValueTask<IReadOnlyList<string>> GetEnabledAsync(CancellationToken ct) =>
        ValueTask.FromResult<IReadOnlyList<string>>(configuration.GetSection("Features").GetChildren()
            .Where(c => bool.TryParse(c.Value, out var on) && on)
            .Select(c => c.Key)
            .ToList());
}

/// <summary>Development email sender: writes the message to the log instead of sending it.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        logger.LogInformation("Email to {To}: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}

/// <summary>Secrets encrypted with ASP.NET Core Data Protection (one protector per purpose).</summary>
public sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private const string Root = "Project.Secrets";

    public string Protect(string purpose, string plaintext) => provider.CreateProtector(Root, purpose).Protect(plaintext);

    public string? Unprotect(string purpose, string protectedValue)
    {
        try
        {
            return provider.CreateProtector(Root, purpose).Unprotect(protectedValue);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}

/// <summary>Used when the Files module is not registered.</summary>
public sealed class NoOpFileService : IFileService
{
    private static readonly Error Disabled = Error.Failure("files.disabled", "File storage is not enabled in this application.");

    public Task<Result<FileDescriptor>> UploadAsync(FileUpload upload, CancellationToken ct) => Task.FromResult<Result<FileDescriptor>>(Disabled);

    public Task<Result<FileContent>> OpenAsync(Guid id, CancellationToken ct) => Task.FromResult<Result<FileContent>>(Disabled);

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct) => Task.FromResult<Result>(Disabled);

    public Task<IReadOnlyList<FileDescriptor>> ListAsync(string entityType, string entityId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<FileDescriptor>>([]);
}

/// <summary>Used when the Notifications module is not registered.</summary>
public sealed class NoOpNotificationSender : INotificationSender
{
    public Task SendAsync(Notify notification, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>Used when the Sequences module is not registered.</summary>
public sealed class NoOpSequenceGenerator : ISequenceGenerator
{
    public Task<string> NextAsync(string sequence, CancellationToken ct) =>
        throw new InvalidOperationException("Document numbering is not enabled: register the Sequences module.");
}
