namespace FairShare.Application.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        /// <summary>The token from the password reset link sent by e-mail.</summary>
        public string Token { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}
