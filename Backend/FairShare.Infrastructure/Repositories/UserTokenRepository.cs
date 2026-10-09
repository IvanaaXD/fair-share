using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class UserTokenRepository : GenericRepository<UserToken>, IUserTokenRepository
{
    public UserTokenRepository(FairShareDbContext context) : base(context)
    {
    }

    // Both queries are tracked on purpose: the caller marks the tokens as used.

    public async Task<UserToken?> GetByHashAsync(
        string tokenHash,
        UserTokenType type,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Type == type, cancellationToken);

    public async Task<IReadOnlyList<UserToken>> GetUsableByUserAsync(
        Guid userId,
        UserTokenType type,
        DateTime now,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Where(t => t.UserId == userId && t.Type == type && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);
}
