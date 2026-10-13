namespace Project.Application.Security;

/// <summary>
/// Encrypts secrets stored in the database (OAuth refresh tokens, provider keys, webhook signing secrets). The default
/// implementation uses ASP.NET Core Data Protection with keys shared by every instance through the platform database.
/// </summary>
public interface ISecretProtector
{
    string Protect(string purpose, string plaintext);

    /// <summary>Null when the value cannot be decrypted (tampered or the key ring was lost).</summary>
    string? Unprotect(string purpose, string protectedValue);
}
