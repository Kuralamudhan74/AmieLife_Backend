using AmieLife.Domain.Enums;

namespace AmieLife.Domain.Entities;

/// <summary>
/// Core user entity. Both registered and guest users live in this table.
/// Guest users: PasswordHash = NULL, IsGuest = true, IsEmailVerified = false.
/// </summary>
public class User
{
    public Guid UserId { get; set; }

    /// <summary>Unique email address. Used as the login identifier.</summary>
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    /// <summary>BCrypt hashed password. NULL for guest users and OAuth users (future).</summary>
    public string? PasswordHash { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>Role determines what the user can access. Default: Customer.</summary>
    public UserRole Role { get; set; } = UserRole.Customer;

    public bool IsEmailVerified { get; set; } = false;

    /// <summary>True for checkout guests. They cannot login with a password.</summary>
    public bool IsGuest { get; set; } = false;

    /// <summary>Tracks consecutive failed login attempts. Resets on success.</summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>If set and in the future, the account is locked.</summary>
    public DateTime? LockoutEndTime { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    // ── Domain logic ─────────────────────────────────────────────────────────

    /// <summary>Returns true if the account is currently locked out.</summary>
    public bool IsLockedOut() => LockoutEndTime.HasValue && LockoutEndTime > DateTime.UtcNow;

    /// <summary>Increments failed attempts and locks account if threshold reached.</summary>
    public void RecordFailedLogin(int maxAttempts, int lockoutMinutes)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEndTime = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Clears lockout state and failed attempt counter after successful login.</summary>
    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockoutEndTime = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
