using Project.Server.Entities.Interfaces;

namespace Project.Server.Entities.Models
{
    /// <summary>
    /// Persistent refresh-token record.
    ///
    /// Security notes:
    /// <list type="bullet">
    ///   <item>Only the SHA-256 hash is stored — the plain token is only ever
    ///         emitted in the HttpOnly cookie or in the login response body.</item>
    ///   <item>Rotation: every successful refresh creates a new row and marks
    ///         the predecessor as replaced (ReplacedByHash). Reuse of a replaced
    ///         or revoked token is treated as compromise: every token in the
    ///         user's chain is revoked (B18).</item>
    /// </list>
    /// </summary>
    public class RefreshToken : IEntity<long>
    {
        public long Id { get; set; }

        /// <summary>The owning user.</summary>
        public long UserId { get; set; }

        /// <summary>BCrypt/SHA-256 hash of the refresh-token value.</summary>
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>UTC expiry. Hard limit; refresh attempts after this fail.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>UTC revocation timestamp. Null while still active.</summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>Hash of the token that replaced this one (rotation chain).</summary>
        public string? ReplacedByHash { get; set; }

        /// <summary>Origin — useful for forensic logging.</summary>
        public string? CreatedByIp { get; set; }

        public string? RevokedByIp { get; set; }

        /// <summary>True when <see cref="RevokedAt"/> is set or <see cref="ExpiresAt"/> is in the past.</summary>
        public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

        public int State { get; set; } = 1;
        public long CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual User? User { get; set; }
    }
}