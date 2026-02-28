using AmieLife.Domain.Entities;

namespace AmieLife.Application.Common.Interfaces;

public interface IEmailVerificationTokenRepository
{
    Task AddAsync(EmailVerificationToken token, CancellationToken ct = default);
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task UpdateAsync(EmailVerificationToken token, CancellationToken ct = default);
}
