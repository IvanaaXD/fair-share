using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

/// <summary>
/// A single-use secret issued to a user: account activation link, password reset link or
/// refresh token. Only the SHA-256 hash of the secret is stored, so a leaked database does not
/// reveal usable tokens (the same idea as storing password hashes instead of passwords).
/// </summary>
public class UserToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public UserTokenType Type { get; set; }

    /// <summary>SHA-256 hash (64 hex characters) of the secret that was sent to the user.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the token is used, replaced by a newer one, or revoked. A token is never reused.</summary>
    public DateTime? UsedAt { get; set; }

    // Optimistic concurrency token (PostgreSQL "xmin"): if two requests try to use the same
    // token at the same moment, only one of them succeeds.
    public uint Version { get; set; }

    public bool IsUsable(DateTime now) => UsedAt is null && ExpiresAt > now;

    public void MarkUsed(DateTime now) => UsedAt = now;
}
