namespace FairShare.PaymentSimulator.Domain;

public enum ChargeStatus
{
    Pending,
    Succeeded,
    Failed
}

/// <summary>
/// One charge of a card. Created as Pending when FairShare asks for it, completed a few seconds
/// later by the ChargeProcessor (like a real bank), and then reported back with a webhook.
/// </summary>
public class Charge
{
    /// <summary>"ch_..."</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Sent by FairShare with every request. The same key always returns the same charge, so a
    /// request repeated after a network error can never charge the card twice (unique index).
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public Guid CardTokenId { get; set; }
    public CardToken CardToken { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;

    /// <summary>FairShare's own id of the payment; sent back in the webhook.</summary>
    public string Reference { get; set; } = string.Empty;

    public ChargeStatus Status { get; set; } = ChargeStatus.Pending;
    public string? FailureCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ---------- webhook delivery ----------

    /// <summary>"evt_..." - one event per completed charge; FairShare ignores an event it has already seen.</summary>
    public string? EventId { get; set; }

    public int WebhookAttempts { get; set; }
    public DateTime? NextWebhookAttemptAt { get; set; }
    public DateTime? WebhookDeliveredAt { get; set; }
    public string? LastWebhookError { get; set; }

    public void Complete(DateTime utcNow)
    {
        if (Status != ChargeStatus.Pending)
            return;

        var outcome = CardToken.IsExpiredAt(utcNow) ? ChargeFailureCodes.ExpiredCard : CardToken.Outcome;

        Status = outcome == ChargeFailureCodes.Success ? ChargeStatus.Succeeded : ChargeStatus.Failed;
        FailureCode = Status == ChargeStatus.Failed ? outcome : null;
        CompletedAt = utcNow;
        EventId = Ids.New("evt");
        NextWebhookAttemptAt = utcNow;
    }

    /// <summary>
    /// Retry schedule of a webhook that FairShare did not accept: 5 s, 10 s, 20 s ... at most
    /// 10 minutes apart. After MaxWebhookAttempts the charge stays undelivered; FairShare can
    /// still learn the result by asking for the charge (GET /v1/charges/{id}).
    /// </summary>
    public void WebhookFailed(DateTime utcNow, string error)
    {
        WebhookAttempts++;
        LastWebhookError = error.Length > 300 ? error[..300] : error;
        var delay = TimeSpan.FromSeconds(Math.Min(5 * Math.Pow(2, WebhookAttempts - 1), 600));
        NextWebhookAttemptAt = utcNow + delay;
    }

    public void WebhookDelivered(DateTime utcNow)
    {
        WebhookAttempts++;
        WebhookDeliveredAt = utcNow;
        LastWebhookError = null;
        NextWebhookAttemptAt = null;
    }
}
