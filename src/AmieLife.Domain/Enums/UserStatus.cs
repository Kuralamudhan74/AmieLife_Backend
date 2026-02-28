namespace AmieLife.Domain.Enums;

/// <summary>
/// Lifecycle status of a user account.
/// Suspended/Deleted enable soft-delete and account management without data loss.
/// </summary>
public enum UserStatus
{
    Active,
    Suspended,
    Deleted
}
