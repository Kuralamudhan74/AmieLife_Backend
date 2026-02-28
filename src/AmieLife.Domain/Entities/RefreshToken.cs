namespace AmieLife.Domain.Entities;

/// <summary>
/// Stores hashed refresh tokens. One user can have multiple active tokens
/// (multiple devices / browsers). On refresh, the used token is revoked and
/// a new one is issued (rotation strategy).
/// </summary>
public class RefreshToken
{
    public Guid RefreshTokenId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the actual token value. Never store raw tokens.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;

    // ── Domain logic ─────────────────────────────────────────────────────────

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive() => !IsRevoked && !IsExpired();

    public void Revoke()
    {
        IsRevoked = true;
    }
}
