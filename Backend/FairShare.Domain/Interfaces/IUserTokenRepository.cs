using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Interfaces;

public interface IUserTokenRepository : IRepository<UserToken>
{
    /// <summary>Finds a token by the hash of its secret, together with its user (tracked).</summary>
    Task<UserToken?> GetByHashAsync(
        string tokenHash,
        UserTokenType type,
        CancellationToken cancellationToken = default);

    /// <summary>The user's tokens of the given type that are neither used nor expired (tracked).</summary>
    Task<IReadOnlyList<UserToken>> GetUsableByUserAsync(
        Guid userId,
        UserTokenType type,
        DateTime now,
        CancellationToken cancellationToken = default);
}
