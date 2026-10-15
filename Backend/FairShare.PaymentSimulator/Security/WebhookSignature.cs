using System.Security.Cryptography;
using System.Text;

namespace FairShare.PaymentSimulator.Security;

/// <summary>
/// Signature of a webhook, in the style of real payment providers:
///     FairShare-Signature: t=1760512345,v1=5f2c...
/// v1 = hex(HMAC-SHA256(secret, "{t}.{body}")). The timestamp is part of the signed text, so an
/// old webhook cannot be replayed with a new time, and FairShare rejects old timestamps.
/// </summary>
public static class WebhookSignature
{
    public const string HeaderName = "FairShare-Signature";

    public static string Create(string secret, string body, DateTimeOffset now)
    {
        var timestamp = now.ToUnixTimeSeconds();
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}"));
        return $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
