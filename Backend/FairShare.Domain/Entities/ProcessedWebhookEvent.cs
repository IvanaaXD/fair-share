namespace FairShare.Domain.Entities;

/// <summary>
/// A webhook event that was already handled. The payment simulator repeats a webhook until it
/// gets an answer, and may send the same event twice; the unique EventId makes sure an event
/// changes a payment only once (idempotency).
/// </summary>
public class ProcessedWebhookEvent : BaseEntity
{
    /// <summary>"evt_..." from the simulator.</summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>"charge.succeeded" / "charge.failed".</summary>
    public string Type { get; set; } = string.Empty;

    public string ChargeId { get; set; } = string.Empty;

    /// <summary>The payment it was applied to; null if the event did not match any payment.</summary>
    public Guid? PaymentId { get; set; }
}
