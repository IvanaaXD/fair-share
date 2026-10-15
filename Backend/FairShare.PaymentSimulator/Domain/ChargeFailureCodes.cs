namespace FairShare.PaymentSimulator.Domain;

/// <summary>The same codes FairShare knows (PaymentErrorCodes there).</summary>
public static class ChargeFailureCodes
{
    public const string Success = "ok";
    public const string CardDeclined = "card_declined";
    public const string InsufficientFunds = "insufficient_funds";
    public const string ExpiredCard = "expired_card";
    public const string InvalidCard = "invalid_card";
    public const string ProcessingError = "processing_error";
}
