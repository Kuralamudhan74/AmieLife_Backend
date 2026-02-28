namespace AmieLife.Shared.Constants;

/// <summary>
/// Application-wide constants. These are NOT secrets — they are fixed policy values.
/// Secrets live in appsettings / environment variables only.
/// </summary>
public static class AppConstants
{
    public static class Auth
    {
        public const int MaxFailedLoginAttempts = 5;
        public const int LockoutDurationMinutes = 15;
        public const int AccessTokenExpirationMinutes = 15;
        public const int RefreshTokenExpirationDays = 14;
        public const int EmailVerificationTokenExpirationHours = 24;
        public const int PasswordResetTokenExpirationMinutes = 30;
        public const int BcryptWorkFactor = 12;
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
    }

    public static class Policy
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireCustomer = "RequireCustomer";
    }

    public static class RateLimit
    {
        /// <summary>Max login attempts per window per IP.</summary>
        public const int LoginMaxRequests = 10;

        /// <summary>Sliding window in seconds for the login rate limiter.</summary>
        public const int LoginWindowSeconds = 60;
    }
}
