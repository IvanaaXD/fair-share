namespace FairShare.Application.Abstractions;

/// <summary>
/// The payment provider (in this project: the payment simulator in Infrastructure/Payments).
/// The application never keeps card numbers or CVV codes: a card is exchanged for a token
/// once, and every later charge uses only the token - the way real providers work (PCI-DSS).
/// </summary>
public interface IPaymentGateway
{
    Task<CardTokenizationResult> TokenizeCardAsync(CardDetails card, CancellationToken cancellationToken = default);

    /// <summary>
    /// Charges the card. The same idempotency key always gives the same result, so a request
    /// repeated after a network error can never charge twice.
    /// </summary>
    Task<ChargeResult> ChargeAsync(
        string cardToken,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

/// <summary>Card data as typed by the user - only passed through to the provider, never stored.</summary>
public sealed record CardDetails(string Number, int ExpiryMonth, int ExpiryYear, string Cvv, string? HolderName);

public sealed record CardTokenizationResult(bool Success, string? Token, string? Brand, string? Last4Digits, string? ErrorCode);

public sealed record ChargeResult(bool Success, string? TransactionId, string? ErrorCode);

/// <summary>Error codes the provider can return (also used by the frontend to pick a message).</summary>
public static class PaymentErrorCodes
{
    public const string CardDeclined = "card_declined";
    public const string InsufficientFunds = "insufficient_funds";
    public const string ExpiredCard = "expired_card";
    public const string InvalidCard = "invalid_card";
    public const string ProcessingError = "processing_error";
}
