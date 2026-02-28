using System.Security.Cryptography;
using System.Text;

namespace AmieLife.Shared.Helpers;

/// <summary>
/// Provides deterministic, one-way hashing utilities for tokens.
/// Do NOT use this for password hashing — use BCrypt for passwords.
/// This is for refresh tokens, email verification tokens, and password reset tokens.
/// </summary>
public static class HashHelper
{
    /// <summary>
    /// Generates a cryptographically secure random token string.
    /// </summary>
    public static string GenerateSecureToken(int byteLength = 64)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// Produces a SHA-256 hex hash of the raw token value.
    /// This hash is what gets stored in the database.
    /// </summary>
    public static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
