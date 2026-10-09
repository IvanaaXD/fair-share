namespace FairShare.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        /// <summary>ISO 4217 code, e.g. BAM, EUR, RSD.</summary>
        public string DefaultCurrency { get; set; } = "BAM";
    }
}
