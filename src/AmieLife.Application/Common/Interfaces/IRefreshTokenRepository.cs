using AmieLife.Domain.Entities;

namespace AmieLife.Application.Common.Interfaces;

/// <summary>
/// Repository abstraction for RefreshToken persistence.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Revokes all active refresh tokens for a user (password change / logout-all).</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}
