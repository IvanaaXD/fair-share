using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>The payment created by an earlier request with the same key (tracked, with card and settlement).</summary>
    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>The payment of a settlement, if there was an attempt (tracked, with card).</summary>
    Task<Payment?> GetBySettlementAsync(Guid settlementTransactionId, CancellationToken cancellationToken = default);
}
