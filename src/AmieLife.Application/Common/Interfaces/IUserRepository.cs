using AmieLife.Domain.Entities;

namespace AmieLife.Application.Common.Interfaces;

/// <summary>
/// Repository abstraction for User persistence operations.
/// Implementations live in Infrastructure — Application layer depends only on this interface.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
