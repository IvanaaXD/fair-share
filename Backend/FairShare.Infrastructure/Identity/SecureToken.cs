using System.Security.Cryptography;
using System.Text;

namespace FairShare.Infrastructure.Identity
{
    /// <summary>Generation and hashing of the secrets used in activation links, reset links and refresh tokens.</summary>
    public static class SecureToken
    {
        /// <summary>
        /// 256 bits from a cryptographically secure generator, written as URL-safe Base64
        /// (43 characters, safe to put in a link without escaping).
        /// </summary>
        public static string Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        /// <summary>
        /// SHA-256 as 64 hex characters. A fast hash is enough here: unlike a password, the token
        /// is 256 random bits, so it cannot be guessed by trying candidates (no bcrypt needed).
        /// </summary>
        public static string Hash(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
