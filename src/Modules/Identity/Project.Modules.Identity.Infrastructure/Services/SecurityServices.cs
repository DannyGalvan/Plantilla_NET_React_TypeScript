using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Project.Infrastructure.Tenancy;
using Project.Modules.Identity.Application;
using Project.Web.Security;

namespace Project.Modules.Identity.Infrastructure.Services;

/// <summary>BCrypt with a configurable work factor; old hashes are upgraded transparently at sign-in.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;
    private static readonly Lazy<string> DummyHash = new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor));

    public string TimingSafeDummyHash => DummyHash.Value;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public bool NeedsRehash(string hash) => BCrypt.Net.BCrypt.PasswordNeedsRehash(hash, WorkFactor);
}

/// <summary>HS256 access tokens. Claims: sub, name, email, tid, ou, pv, role, jti. No permissions.</summary>
public sealed class JwtTokenService(IOptionsMonitor<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(options.CurrentValue.RefreshTokenDays);

    public AccessToken CreateAccessToken(AccessTokenRequest request)
    {
        var settings = options.CurrentValue;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new("name", request.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(TenantClaims.TenantId, request.TenantId.ToString()),
            new(TenantClaims.PermissionsVersion, request.PermissionsVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };

        if (!string.IsNullOrEmpty(request.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, request.Email));
        }

        if (request.OrganizationUnitId is { } unit)
        {
            claims.Add(new Claim(TenantClaims.OrganizationUnitId, unit.ToString()));
        }

        claims.AddRange(request.Roles.Select(r => new Claim("role", r)));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(settings.GetSecurityKey(), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expires);
    }
}

/// <summary>256-bit random tokens, URL safe, stored as SHA-256 hex.</summary>
public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    public (string Token, string Hash) Generate()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
