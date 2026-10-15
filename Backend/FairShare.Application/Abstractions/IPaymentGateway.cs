namespace FairShare.Application.Abstractions;

/// <summary>
/// The payment provider - in this project the separate payment simulator service
/// (FairShare.PaymentSimulator), reached over HTTP by Infrastructure/Payments/PaymentSimulatorClient.
///
/// The application never keeps card numbers or CVV codes: a card is exchanged for a token
/// once, and every later charge uses only the token - the way real providers work (PCI-DSS).
/// Charges are asynchronous: the provider accepts a charge at once and reports the result
/// later with a signed webhook (POST /api/webhooks/payments).
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Throws ServiceUnavailableException if the provider cannot be reached.</summary>
    Task<CardTokenizationResult> TokenizeCardAsync(CardDetails card, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the provider to charge the card. The answer only says whether the charge was
    /// accepted for processing. The same idempotency key always returns the same charge, so a
    /// request repeated after a network error can never charge twice.
    /// </summary>
    Task<ChargeResult> ChargeAsync(GatewayChargeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Current state of a charge - used when a webhook did not arrive in time. Null if the
    /// provider has no such charge; throws ServiceUnavailableException if it cannot be reached.
    /// </summary>
    Task<ChargeStatusResult?> GetChargeAsync(string chargeId, CancellationToken cancellationToken = default);

    /// <summary>The charge created with an idempotency key (when its id never reached us).</summary>
    Task<ChargeStatusResult?> FindChargeAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

/// <summary>Card data as typed by the user - only passed through to the provider, never stored.</summary>
public sealed record CardDetails(string Number, int ExpiryMonth, int ExpiryYear, string Cvv, string? HolderName);

public sealed record CardTokenizationResult(bool Success, string? Token, string? Brand, string? Last4Digits, string? ErrorCode);

/// <param name="Reference">Our payment id; the provider returns it in the webhook.</param>
public sealed record GatewayChargeRequest(string CardToken, decimal Amount, string Currency, string IdempotencyKey, string Reference);

/// <param name="Accepted">The provider took the charge for processing (the result comes later).</param>
public sealed record ChargeResult(bool Accepted, string? ChargeId, string? ErrorCode);

public enum ChargeOutcome
{
    Pending,
    Succeeded,
    Failed
}

public sealed record ChargeStatusResult(string ChargeId, ChargeOutcome Outcome, string? FailureCode);

/// <summary>Error codes the provider can return (also used by the frontend to pick a message).</summary>
public static class PaymentErrorCodes
{
    public const string CardDeclined = "card_declined";
    public const string InsufficientFunds = "insufficient_funds";
    public const string ExpiredCard = "expired_card";
    public const string InvalidCard = "invalid_card";
    public const string ProcessingError = "processing_error";

    /// <summary>The provider could not be reached; nothing is known to be charged.</summary>
    public const string GatewayUnavailable = "gateway_unavailable";
}
