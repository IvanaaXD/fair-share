namespace FairShare.PaymentSimulator;

/// <summary>Section "Simulator" of the configuration. Secrets come from user-secrets or environment variables.</summary>
public class SimulatorOptions
{
    public const string SectionName = "Simulator";

    /// <summary>The key FairShare sends in the X-Api-Key header (the merchant's secret key).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Where results are reported, e.g. https://localhost:7001/api/webhooks/payments.</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>Key of the HMAC-SHA256 signature of every webhook; FairShare knows the same key.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>How long "the bank" takes to answer.</summary>
    public int ProcessingDelaySeconds { get; set; } = 3;

    public int MaxWebhookAttempts { get; set; } = 10;
}
