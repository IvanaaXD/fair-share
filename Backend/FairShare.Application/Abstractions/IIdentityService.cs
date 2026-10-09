using FairShare.Application.DTOs.Auth;

namespace FairShare.Application.Abstractions
{
    /// <summary>
    /// Registration, sign-in and everything around the user's credentials. Failures are reported
    /// by exceptions (turned into HTTP status codes by ExceptionHandlingMiddleware), like in all
    /// other services.
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>Creates an inactive account and e-mails the activation link.</summary>
        Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        /// <summary>Activates the account that the activation token belongs to.</summary>
        Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);

        /// <summary>Sends a new activation link. Does nothing (silently) if the address is unknown or already active.</summary>
        Task ResendConfirmationAsync(EmailRequest request, CancellationToken cancellationToken = default);

        /// <summary>Checks the credentials and returns an access token and a refresh token.</summary>
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>Exchanges a refresh token for a new access token and a new refresh token (rotation).</summary>
        Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

        /// <summary>Revokes the refresh token, so it can no longer be used to obtain access tokens.</summary>
        Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

        /// <summary>E-mails a password reset link. Does nothing (silently) if the address is unknown.</summary>
        Task ForgotPasswordAsync(EmailRequest request, CancellationToken cancellationToken = default);

        /// <summary>Sets a new password using the token from the reset link.</summary>
        Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    }
}
