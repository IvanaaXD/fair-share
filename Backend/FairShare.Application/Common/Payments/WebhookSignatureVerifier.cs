using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FairShare.Application.Common.Payments;

/// <summary>
/// Checks the signature the payment simulator puts on every webhook:
///     FairShare-Signature: t=1760512345,v1=5f2c...
/// v1 = hex(HMAC-SHA256(secret, "{t}.{body}")). Without the shared secret nobody can produce
/// a valid signature, so a forged "the payment succeeded" request is rejected. The timestamp
/// is signed too and must be recent, so a captured webhook cannot be replayed later.
/// </summary>
public static class WebhookSignatureVerifier
{
    public const string HeaderName = "FairShare-Signature";

    public static bool IsValid(string secret, string body, string? header, DateTimeOffset now, TimeSpan tolerance)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrWhiteSpace(header))
            return false;

        string? timestamp = null;
        string? signature = null;
        foreach (var part in header.Split(','))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2)
                continue;
            if (pair[0].Trim() == "t")
                timestamp = pair[1].Trim();
            else if (pair[0].Trim() == "v1")
                signature = pair[1].Trim();
        }

        if (timestamp is null || signature is null
            || !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            return false;

        if (Math.Abs(now.ToUnixTimeSeconds() - seconds) > tolerance.TotalSeconds)
            return false;

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}"));
        var expectedHex = Encoding.ASCII.GetBytes(Convert.ToHexString(expected).ToLowerInvariant());
        var givenHex = Encoding.ASCII.GetBytes(signature.ToLowerInvariant());

        // Constant-time comparison: the time taken does not reveal how much of the signature was right.
        return CryptographicOperations.FixedTimeEquals(expectedHex, givenHex);
    }
}
