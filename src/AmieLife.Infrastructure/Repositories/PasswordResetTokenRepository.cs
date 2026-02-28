using AmieLife.Application.Common.Interfaces;
using AmieLife.Domain.Entities;
using AmieLife.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AmieLife.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;

    public PasswordResetTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        await _db.PasswordResetTokens.AddAsync(token, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await _db.PasswordResetTokens.FirstOrDefaultAsync(p => p.TokenHash == tokenHash, ct);

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _db.PasswordResetTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }
}
