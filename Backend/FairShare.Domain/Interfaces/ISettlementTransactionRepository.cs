using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Interfaces;

public interface ISettlementTransactionRepository : IRepository<SettlementTransaction>
{
    /// <summary>Предложене/измирене трансакције поравнања за групу (функционалности 5.7 и 5.8).</summary>
    Task<IReadOnlyList<SettlementTransaction>> GetByGroupAsync(
        Guid groupId,
        SettlementStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>Учитава трансакцију заједно са повезаним плаћањем (за провјеру идемпотентности).</summary>
    Task<SettlementTransaction?> GetWithPaymentAsync(
        Guid settlementTransactionId,
        CancellationToken cancellationToken = default);
}
