using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using Project.Application;
using Project.Modules.Identity.Application;

namespace Project.Modules.Identity.Infrastructure;

public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly TimeProvider _clock;
    private readonly JwtSettings _settings;

    public JwtTokenIssuer(TimeProvider clock, JwtSettings settings)
    {
        _clock = clock;
        _settings = settings;
    }

    public Task<IssuedToken> IssueAsync(TokenDescriptor descriptor, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var expires = now.Add(_settings.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, descriptor.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, descriptor.UserName),
            new("pv", descriptor.PermissionsVersion.ToString()),
            new("tid", descriptor.TenantId?.ToString() ?? string.Empty),
            new("ou", descriptor.OrganizationUnitId?.ToString() ?? string.Empty),
        };

        foreach (var role in descriptor.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(new IssuedToken(jwt, expires, now));
    }
}

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "Plantilla";
    public string Audience { get; set; } = "Plantilla.Client";
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
    public string SigningKey { get; set; } = string.Empty;
}

public sealed class Sha256RefreshTokenService : IRefreshTokenService
{
    public (string PlainToken, string Hash) Generate()
    {
        var buffer = RandomNumberGenerator.GetBytes(48);
        var plain = Convert.ToBase64String(buffer);
        var hash = Hash(plain);
        return (plain, hash);
    }

    public string Hash(string plainToken)
    {
        var bytes = Encoding.UTF8.GetBytes(plainToken);
        var digest = SHA256.HashData(bytes);
        return Convert.ToHexString(digest);
    }

    public bool IsExpired(DateTimeOffset expiresAt, DateTimeOffset? revokedAt) =>
        revokedAt is not null || expiresAt <= DateTimeOffset.UtcNow;
}

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string hashed, string provided) =>
        BCrypt.Net.BCrypt.Verify(provided, hashed);

    public bool NeedsRehash(string hashed) =>
        BCrypt.Net.BCrypt.PasswordNeedsRehash(hashed, WorkFactor);
}

public sealed class DefaultPasswordPolicy : IPasswordPolicy
{
    public PasswordPolicyResult Validate(string password)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            errors["Password"] = new[] { "Password must be at least 8 characters long." };
        }
        if (!password.Any(char.IsUpper))
        {
            errors["Password"] = Append(errors, "Password", "Password must contain an uppercase letter.");
        }
        if (!password.Any(char.IsLower))
        {
            errors["Password"] = Append(errors, "Password", "Password must contain a lowercase letter.");
        }
        if (!password.Any(char.IsDigit))
        {
            errors["Password"] = Append(errors, "Password", "Password must contain a digit.");
        }
        if (password.All(char.IsLetterOrDigit))
        {
            errors["Password"] = Append(errors, "Password", "Password must contain a non-alphanumeric character.");
        }

        return errors.Count == 0 ? PasswordPolicyResult.Success : new PasswordPolicyResult(false, errors);
    }

    private static string[] Append(Dictionary<string, string[]> dict, string key, string message)
    {
        var current = dict.TryGetValue(key, out var existing) ? existing : Array.Empty<string>();
        return current.Concat(new[] { message }).ToArray();
    }
}

public sealed class DefaultLockoutPolicy : ILockoutPolicy
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    public bool IsLockedOut(int failedAttempts, DateTimeOffset? lockoutEnd, DateTimeOffset now) =>
        failedAttempts >= MaxAttempts && lockoutEnd is not null && lockoutEnd > now;

    public DateTimeOffset ComputeLockoutEnd(int failedAttempts, DateTimeOffset now) =>
        failedAttempts >= MaxAttempts ? now.Add(LockoutDuration) : now;

    public int IncrementFailedAttempts(int current) => current + 1;

    public int ResetFailedAttempts() => 0;
}

public sealed class HybridCachePermissionService : IPermissionService
{
    private readonly ICacheService _cache;
    private readonly IPermissionCatalog _catalog;

    public HybridCachePermissionService(ICacheService cache, IPermissionCatalog catalog)
    {
        _cache = cache;
        _catalog = catalog;
    }

    public async ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        Guid? tenantId, Guid userId, long permissionsVersion, Guid? activeOrganizationUnitId, CancellationToken ct)
    {
        var key = BuildCacheKey(tenantId, userId, permissionsVersion);
        var cached = await _cache.GetAsync<HashSet<string>>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var fresh = await _catalog.GetPermissionsForUserAsync(userId, ct);
        await _cache.SetAsync(key, fresh, tags: new[] { $"user:{userId}", $"perm:{tenantId}:{userId}" }, ct: ct);
        return fresh;
    }

    public async ValueTask<bool> HasAsync(
        Guid? tenantId, Guid userId, long permissionsVersion, Guid? activeOrganizationUnitId, string permission, CancellationToken ct)
    {
        var set = await GetEffectivePermissionsAsync(tenantId, userId, permissionsVersion, activeOrganizationUnitId, ct);
        return set.Contains(permission);
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken ct)
    {
        await _cache.RemoveByTagsAsync(new[] { $"user:{userId}" }, ct);
    }

    private static string BuildCacheKey(Guid? tenantId, Guid userId, long permissionsVersion) =>
        $"perm:{(tenantId?.ToString() ?? "default")}:{userId}:{permissionsVersion}";
}

/// <summary>
/// Source of truth for the user's permissions. Reads from the Identity module's
/// tables via the EF repository. Phase 4 ships the interface only.
/// </summary>
public interface IPermissionCatalog
{
    Task<HashSet<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken ct);
}