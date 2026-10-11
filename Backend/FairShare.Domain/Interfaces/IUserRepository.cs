using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Search and pagination for administrator review of users (feature 5.2).</summary>
    Task<IReadOnlyList<User>> SearchAsync(
    string? searchTerm,
        bool? isBlocked,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
