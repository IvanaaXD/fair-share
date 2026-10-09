namespace FairShare.Infrastructure.Identity
{
    /// <summary>
    /// Bound to the "AuthSettings" section of appsettings.json. Every value has a default,
    /// so the application also works when the section is missing.
    /// </summary>
    public class AuthSettings
    {
        /// <summary>Address of the web application; activation and reset links in e-mails point to it.</summary>
        public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

        /// <summary>
        /// Lifetime of the JWT access token. The default keeps the previous behaviour (3 hours);
        /// with refresh tokens in place a short value such as 15 is recommended.
        /// </summary>
        public int AccessTokenMinutes { get; set; } = 180;

        public int RefreshTokenDays { get; set; } = 7;

        public int EmailConfirmationHours { get; set; } = 24;

        public int PasswordResetMinutes { get; set; } = 30;
    }
}
