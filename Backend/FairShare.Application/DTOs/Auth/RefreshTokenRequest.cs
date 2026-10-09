namespace FairShare.Application.DTOs.Auth
{
    /// <summary>Used by "refresh" (exchange the token) and "logout" (revoke the token).</summary>
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
