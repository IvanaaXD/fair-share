using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Auth;
using FairShare.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FairShare.API.Controllers
{
    /// <summary>
    /// Everything a user does before (or without) being signed in. Failures are thrown as
    /// exceptions by IdentityService and converted to status codes by ExceptionHandlingMiddleware:
    /// 400 invalid/expired link, 401 wrong credentials or session, 403 blocked or inactive account,
    /// 409 e-mail already registered, 429 too many attempts.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        /// <summary>Creates an inactive account and sends the activation link by e-mail.</summary>
        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            await _identityService.RegisterAsync(request, cancellationToken);
            return Ok(new { message = "Налог је креиран. Линк за активацију је послат на вашу e-mail адресу." });
        }

        /// <summary>Activates the account using the token from the activation link.</summary>
        [HttpPost("confirm-email")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
        {
            await _identityService.ConfirmEmailAsync(request, cancellationToken);
            return Ok(new { message = "Налог је активиран. Сада се можете пријавити." });
        }

        /// <summary>Sends a new activation link. The answer is the same whether or not the address is registered.</summary>
        [HttpPost("resend-confirmation")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<IActionResult> ResendConfirmation([FromBody] EmailRequest request, CancellationToken cancellationToken)
        {
            await _identityService.ResendConfirmationAsync(request, cancellationToken);
            return Ok(new { message = "Ако налог са овом адресом чека активацију, послат је нови линк." });
        }

        /// <summary>Signs in. Limited to 10 attempts per minute per IP address, which makes guessing passwords impractical.</summary>
        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _identityService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Exchanges a refresh token for a new access token and a new refresh token. Only the
        /// global limit applies: a refresh token is a 256-bit secret, so it cannot be guessed, and
        /// clients behind one IP address (e.g. a university network) refresh often.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var result = await _identityService.RefreshAsync(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>Revokes the refresh token. The access token simply expires on its own.</summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            await _identityService.LogoutAsync(request, cancellationToken);
            return NoContent();
        }

        /// <summary>Sends a password reset link. The answer is the same whether or not the address is registered.</summary>
        [HttpPost("forgot-password")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailRequest request, CancellationToken cancellationToken)
        {
            await _identityService.ForgotPasswordAsync(request, cancellationToken);
            return Ok(new { message = "Ако налог са овом адресом постоји, послат је линк за промјену лозинке." });
        }

        /// <summary>Sets a new password using the token from the reset link.</summary>
        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            await _identityService.ResetPasswordAsync(request, cancellationToken);
            return Ok(new { message = "Лозинка је промијењена. Пријавите се новом лозинком." });
        }
    }
}
