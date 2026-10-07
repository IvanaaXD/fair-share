using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Interfaces;

public interface ISettlementTransactionRepository : IRepository<SettlementTransaction>
{
    /// <summary>Read-only list with debtor, creditor and QR data - for displaying settlements.</summary>
    Task<IReadOnlyList<SettlementTransaction>> GetByGroupAsync(
        Guid groupId,
        SettlementStatus? status = null,
        CancellationToken cancellationToken = default);

    // NEW: tracked transactions WITHOUT related entities - for deleting them. Entities from the
    // read-only list above must not be passed to Remove(): they carry their own copies of the
    // users, which clash with users the DbContext already tracks.
    Task<IReadOnlyList<SettlementTransaction>> GetForUpdateByGroupAsync(
        Guid groupId,
        SettlementStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<SettlementTransaction?> GetWithPaymentAsync(
        Guid settlementTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked transaction with debtor, creditor and QR data.</summary>
    Task<SettlementTransaction?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
