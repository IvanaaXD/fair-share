namespace FairShare.Application.DTOs.Auth
{
    public class ConfirmEmailRequest
    {
        /// <summary>The token from the activation link sent by e-mail.</summary>
        public string Token { get; set; } = string.Empty;
    }
}
