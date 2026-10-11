using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>Prevents duplicate processing of the same request to the Payment Simulator (feature 5.9).</summary>
    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<Payment?> GetBySettlementTransactionAsync(
        Guid settlementTransactionId,
        CancellationToken cancellationToken = default);
}
