using FairShare.Application.DTOs.Payments;

namespace FairShare.Application.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// The debtor pays a proposed settlement with a saved card. On success the settlement is
    /// marked as settled; a declined card returns a payment with Status = Failed and a reason.
    /// </summary>
    Task<PaymentResponse> PayWithCardAsync(
        Guid groupId,
        Guid settlementTransactionId,
        PayWithCardRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
