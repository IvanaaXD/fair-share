using FairShare.Application.DTOs.Payments;

namespace FairShare.Application.Interfaces;

public interface IPaymentCardService
{
    Task<IReadOnlyList<PaymentCardResponse>> GetMyCardsAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Tokenizes the card with the payment simulator and saves only the token and display data.</summary>
    Task<PaymentCardResponse> AddCardAsync(Guid currentUserId, AddPaymentCardRequest request, CancellationToken cancellationToken = default);

    Task DeleteCardAsync(Guid currentUserId, Guid paymentCardId, CancellationToken cancellationToken = default);
}
