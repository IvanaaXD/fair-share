using FairShare.Application.DTOs.Payments;

namespace FairShare.Application.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// The debtor pays a proposed settlement with a saved card. The provider only accepts the
    /// charge here, so the payment usually comes back with Status = Pending; the result arrives
    /// with the webhook. A charge the provider refuses at once comes back as Failed.
    /// </summary>
    Task<PaymentResponse> PayWithCardAsync(
        Guid groupId,
        Guid settlementTransactionId,
        PayWithCardRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Current state of the card payment of a settlement (debtor or creditor). If a payment has
    /// been pending for a while without a webhook, the provider is asked directly.
    /// </summary>
    Task<PaymentResponse> GetPaymentStatusAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Result of a charge reported by the provider. The signature is checked by the controller.
    /// The same event twice changes nothing (idempotency).
    /// </summary>
    Task<WebhookOutcome> HandleWebhookAsync(PaymentWebhookRequest webhook, CancellationToken cancellationToken = default);
}
