using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IGroupMemberRepository : IRepository<GroupMember>
{
    Task<GroupMember?> GetAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupMember>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
}
