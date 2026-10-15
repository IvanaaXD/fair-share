namespace FairShare.Application.DTOs.Payments;

/// <summary>Body of a webhook from the payment simulator (POST /api/webhooks/payments).</summary>
public sealed class PaymentWebhookRequest
{
    /// <summary>"evt_..." - unique per event; an event seen before is ignored (idempotency).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>"charge.succeeded" or "charge.failed".</summary>
    public string Type { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
    public PaymentWebhookCharge Data { get; set; } = new();
}

public sealed class PaymentWebhookCharge
{
    /// <summary>"ch_..." - the provider's id of the charge.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>"succeeded", "failed" or "pending".</summary>
    public string Status { get; set; } = string.Empty;

    public string? FailureCode { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;

    /// <summary>Our payment id, sent with the charge.</summary>
    public string Reference { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

/// <summary>What happened with a webhook (returned to the simulator, useful in its log).</summary>
public enum WebhookOutcome
{
    Processed,
    Duplicate,
    Ignored
}
