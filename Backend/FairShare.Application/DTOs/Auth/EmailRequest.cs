namespace FairShare.Application.DTOs.Auth
{
    /// <summary>Used by "forgot password" and "resend activation link" - both need only the e-mail address.</summary>
    public class EmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
