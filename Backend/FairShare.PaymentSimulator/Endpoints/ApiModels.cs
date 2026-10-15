using FairShare.PaymentSimulator.Domain;

namespace FairShare.PaymentSimulator.Endpoints;

/// <summary>Card data as typed by the user. It lives only for the duration of the request.</summary>
public sealed record TokenizeRequest(string? Number, int ExpiryMonth, int ExpiryYear, string? Cvv, string? HolderName);

public sealed record TokenResponse(string Token, string Brand, string Last4Digits, int ExpiryMonth, int ExpiryYear);

/// <param name="Reference">FairShare's id of the payment, returned in the webhook.</param>
public sealed record ChargeRequest(string? Token, decimal Amount, string? Currency, string? Reference);

public sealed record ChargeResponse(
    string Id,
    string Status,
    string? FailureCode,
    decimal Amount,
    string Currency,
    string Reference,
    string IdempotencyKey,
    DateTime CreatedAt,
    DateTime? CompletedAt)
{
    public static ChargeResponse From(Charge charge) => new(
        charge.Id,
        StatusName(charge.Status),
        charge.FailureCode,
        charge.Amount,
        charge.Currency,
        charge.Reference,
        charge.IdempotencyKey,
        charge.CreatedAt,
        charge.CompletedAt);

    public static string StatusName(ChargeStatus status) => status switch
    {
        ChargeStatus.Succeeded => "succeeded",
        ChargeStatus.Failed => "failed",
        _ => "pending"
    };
}
