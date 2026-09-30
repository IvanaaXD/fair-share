using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>Спречава дуплирано процесирање истог захтјева ка Payment Simulator-у (функционалност 5.9).</summary>
    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<Payment?> GetBySettlementTransactionAsync(
        Guid settlementTransactionId,
        CancellationToken cancellationToken = default);
}
