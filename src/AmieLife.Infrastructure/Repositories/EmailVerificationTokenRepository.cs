using AmieLife.Application.Common.Interfaces;
using AmieLife.Domain.Entities;
using AmieLife.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AmieLife.Infrastructure.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AppDbContext _db;

    public EmailVerificationTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(EmailVerificationToken token, CancellationToken ct = default)
    {
        await _db.EmailVerificationTokens.AddAsync(token, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await _db.EmailVerificationTokens.FirstOrDefaultAsync(e => e.TokenHash == tokenHash, ct);

    public async Task UpdateAsync(EmailVerificationToken token, CancellationToken ct = default)
    {
        _db.EmailVerificationTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }
}
