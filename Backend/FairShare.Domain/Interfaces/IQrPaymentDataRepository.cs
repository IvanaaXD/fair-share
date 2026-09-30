using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IQrPaymentDataRepository : IRepository<QrPaymentData>
{
    Task<QrPaymentData?> GetBySettlementTransactionAsync(
        Guid settlementTransactionId,
        CancellationToken cancellationToken = default);
}
