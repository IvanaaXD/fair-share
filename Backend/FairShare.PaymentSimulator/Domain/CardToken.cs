namespace FairShare.PaymentSimulator.Domain;

/// <summary>
/// A tokenized card. The card number and the CVV are never stored - only what a real provider
/// keeps after tokenization: the token, the last four digits, the brand and the expiry date.
/// </summary>
public class CardToken
{
    public Guid Id { get; set; }

    /// <summary>"tok_..." - the only thing FairShare stores about the card.</summary>
    public string Token { get; set; } = string.Empty;

    public string Last4Digits { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string? HolderName { get; set; }

    /// <summary>
    /// What charges of this card end with: "ok" or an error code. Decided at tokenization from
    /// the well-known test card numbers, so the outcome survives restarts without the number.
    /// </summary>
    public string Outcome { get; set; } = ChargeFailureCodes.Success;

    public DateTime CreatedAt { get; set; }

    /// <summary>The card is valid until the end of its expiry month.</summary>
    public bool IsExpiredAt(DateTime utcNow)
        => new DateTime(ExpiryYear, ExpiryMonth, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1) <= utcNow;
}
