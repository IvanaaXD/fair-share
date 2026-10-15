using System.Security.Cryptography;

namespace FairShare.PaymentSimulator.Domain;

/// <summary>Random, unguessable ids with a readable prefix: "tok_...", "ch_...", "evt_...".</summary>
public static class Ids
{
    public static string New(string prefix)
        => $"{prefix}_{Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant()}";
}
