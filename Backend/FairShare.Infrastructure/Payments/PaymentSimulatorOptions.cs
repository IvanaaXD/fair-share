namespace FairShare.Infrastructure.Payments;

/// <summary>
/// Section "PaymentSimulator" of the configuration. The two keys are secrets: user-secrets in
/// development, environment variables in Docker - never in appsettings.json or git.
/// </summary>
public class PaymentSimulatorOptions
{
    public const string SectionName = "PaymentSimulator";

    /// <summary>Address of the simulator service, e.g. http://localhost:5199.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Sent in the X-Api-Key header of every call to the simulator.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Key of the HMAC signature of the simulator's webhooks.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}
