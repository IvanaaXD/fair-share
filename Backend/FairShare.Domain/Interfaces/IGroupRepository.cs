using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IGroupRepository : IRepository<Group>
{
    /// <summary>Учитава групу заједно са члановима (потребно за приказ салда, функционалност 5.7).</summary>
    Task<Group?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
