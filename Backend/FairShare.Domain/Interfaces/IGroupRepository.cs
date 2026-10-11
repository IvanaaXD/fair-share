using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IGroupRepository : IRepository<Group>
{
    /// <summary>Loads the group along with its members </summary>
    Task<Group?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
