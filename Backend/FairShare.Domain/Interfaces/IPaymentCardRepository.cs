using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IPaymentCardRepository : IRepository<PaymentCard>
{
    Task<IReadOnlyList<PaymentCard>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PaymentCard?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}
