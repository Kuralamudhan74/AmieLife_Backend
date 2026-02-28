namespace AmieLife.Domain.Entities;

/// <summary>
/// Short-lived token sent via email to verify user ownership of an address.
/// Token is hashed before storage. The raw value is sent in the email link.
/// </summary>
public class EmailVerificationToken
{
    public Guid TokenId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the raw token sent to the user's email.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Marked true after the user successfully verifies. Cannot be reused.</summary>
    public bool IsUsed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;

    // ── Domain logic ─────────────────────────────────────────────────────────

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid() => !IsUsed && !IsExpired();
}
