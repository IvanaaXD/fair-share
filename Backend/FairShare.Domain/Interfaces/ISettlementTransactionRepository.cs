using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Interfaces;

public interface ISettlementTransactionRepository : IRepository<SettlementTransaction>
{
    Task<IReadOnlyList<SettlementTransaction>> GetByGroupAsync(
        Guid groupId,
        SettlementStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<SettlementTransaction?> GetWithPaymentAsync(
        Guid settlementTransactionId,
        CancellationToken cancellationToken = default);

    // НОВО: учитава трансакцију заједно са дужником, повјериоцем и QR подацима -
    // потребно SettlementService-у за MarkAsSettledAsync (мапирање одговора без
    // додатних упита).
    Task<SettlementTransaction?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
