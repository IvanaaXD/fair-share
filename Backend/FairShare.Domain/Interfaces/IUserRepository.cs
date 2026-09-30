using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Претрага и пагинација за администраторски преглед корисника (функционалност 5.2).</summary>
    Task<IReadOnlyList<User>> SearchAsync(
        string? searchTerm,
        bool? isBlocked,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
