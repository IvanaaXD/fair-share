using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IPaymentCardRepository : IRepository<PaymentCard>
{
    /// <summary>The user's saved cards, newest first (read-only).</summary>
    Task<IReadOnlyList<PaymentCard>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>true if any payment was made with the card (such a card cannot be deleted).</summary>
    Task<bool> HasPaymentsAsync(Guid paymentCardId, CancellationToken cancellationToken = default);
}
