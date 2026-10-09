using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Auth;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FairShare.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private const string InvalidLoginMessage = "Погрешан e-mail или лозинка.";
        private const string InvalidSessionMessage = "Сесија је истекла. Пријавите се поново.";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailQueue _emailQueue;
        private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _configuration;
        private readonly AuthSettings _settings;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IEmailQueue emailQueue,
            IAuditLogService auditLogService,
            IConfiguration configuration,
            IOptions<AuthSettings> settings,
            IHostEnvironment environment,
            ILogger<IdentityService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _emailQueue = emailQueue;
            _auditLogService = auditLogService;
            _configuration = configuration;
            _settings = settings.Value;
            _environment = environment;
            _logger = logger;
        }

        // ---------- registration and account activation ----------

        public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var email = User.NormalizeEmail(request.Email);

            if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
                throw new ConflictException("Налог са овом e-mail адресом већ постоји.");

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                DefaultCurrency = request.DefaultCurrency.ToUpperInvariant(),
                Role = UserRole.Customer,
                // The account cannot be used until the activation link is opened.
                MustConfirmEmail = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);

            var token = await CreateTokenAsync(
                user, UserTokenType.EmailConfirmation,
                TimeSpan.FromHours(_settings.EmailConfirmationHours), cancellationToken);

            // The user and the activation token are saved in one transaction. If two requests
            // register the same e-mail at the same moment, the unique index on Email rejects
            // the second one (HTTP 409).
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await SendActivationEmailAsync(user, token, cancellationToken);
            await _auditLogService.LogAsync(user.Id, "Auth.Register", "User", user.Id, cancellationToken);
        }

        public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var token = await FindUsableTokenAsync(request.Token, UserTokenType.EmailConfirmation, now, cancellationToken)
                ?? throw new BadRequestException("Линк за активацију није исправан или је истекао. Затражите нови.");

            token.MarkUsed(now);
            token.User.MustConfirmEmail = false;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync(token.UserId, "Auth.ConfirmEmail", "User", token.UserId, cancellationToken);
        }

        public async Task ResendConfirmationAsync(EmailRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            // The caller gets the same answer whether or not the address exists, so this endpoint
            // cannot be used to find out which e-mail addresses are registered.
            if (user is null || !user.MustConfirmEmail || user.IsBlocked)
                return;

            var now = DateTime.UtcNow;

            // Only the newest link is valid.
            await RevokeTokensAsync(user.Id, UserTokenType.EmailConfirmation, now, cancellationToken);

            var token = await CreateTokenAsync(
                user, UserTokenType.EmailConfirmation,
                TimeSpan.FromHours(_settings.EmailConfirmationHours), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await SendActivationEmailAsync(user, token, cancellationToken);
        }

        // ---------- sign-in, token refresh, sign-out ----------

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Username, cancellationToken);

            // One answer for an unknown e-mail and for a wrong password, so the response
            // does not reveal which e-mail addresses have an account.
            if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                throw new InvalidCredentialsException(InvalidLoginMessage);

            // Checked only after the password, so the account state is shown to its owner only.
            EnsureCanSignIn(user);

            var response = await IssueTokensAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(user.Id, "Auth.Login", "User", user.Id, cancellationToken);
            return response;
        }

        public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var token = await _unitOfWork.UserTokens.GetByHashAsync(
                    SecureToken.Hash(request.RefreshToken), UserTokenType.RefreshToken, cancellationToken)
                ?? throw new InvalidCredentialsException(InvalidSessionMessage);

            if (token.UsedAt is not null)
            {
                // Rotation: every refresh token is valid exactly once. If a token that was already
                // exchanged shows up again, someone else may have a copy of it, so all sessions of
                // the user are closed and they have to sign in again.
                await RevokeTokensAsync(token.UserId, UserTokenType.RefreshToken, now, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}; all sessions were revoked.", token.UserId);

                throw new InvalidCredentialsException(InvalidSessionMessage);
            }

            if (token.ExpiresAt <= now)
                throw new InvalidCredentialsException(InvalidSessionMessage);

            // A user blocked after signing in must not be able to keep the session alive.
            EnsureCanSignIn(token.User);

            token.MarkUsed(now);
            var response = await IssueTokensAsync(token.User, cancellationToken);

            // The old token is retired and the new one stored in one transaction. The row version
            // on UserToken makes a second, simultaneous exchange of the same token fail (HTTP 409).
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return response;
        }

        public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var token = await _unitOfWork.UserTokens.GetByHashAsync(
                SecureToken.Hash(request.RefreshToken), UserTokenType.RefreshToken, cancellationToken);

            // Signing out twice, or with an unknown token, is not an error.
            if (token is null || !token.IsUsable(now))
                return;

            token.MarkUsed(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // ---------- forgotten password ----------

        public async Task ForgotPasswordAsync(EmailRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            // Same answer whether or not the address exists (see ResendConfirmationAsync).
            if (user is null || user.IsBlocked)
                return;

            var now = DateTime.UtcNow;

            // Only the newest link is valid.
            await RevokeTokensAsync(user.Id, UserTokenType.PasswordReset, now, cancellationToken);

            var token = await CreateTokenAsync(
                user, UserTokenType.PasswordReset,
                TimeSpan.FromMinutes(_settings.PasswordResetMinutes), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var url = BuildFrontendUrl("reset-password", token);
            var body =
                "Примили смо захтјев за промјену лозинке вашег FairShare налога.\n\n" +
                "Нову лозинку можете поставити на сљедећем линку:\n" +
                url + "\n\n" +
                $"Линк важи {_settings.PasswordResetMinutes} min. Ако нисте ви тражили промјену, " +
                "занемарите ову поруку - ваша лозинка остаје иста.";

            await SendEmailAsync(user, "Промјена лозинке", body, url, cancellationToken);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var token = await FindUsableTokenAsync(request.Token, UserTokenType.PasswordReset, now, cancellationToken)
                ?? throw new BadRequestException("Линк за промјену лозинке није исправан или је истекао. Затражите нови.");

            var user = token.User;
            if (user.IsBlocked)
                throw new ForbiddenException("Ваш налог је блокиран. Обратите се администратору.");

            user.ChangePassword(_passwordHasher.HashPassword(request.NewPassword));
            user.MustChangePassword = false;

            // Opening a link that was sent to the mailbox proves the user owns the address,
            // exactly like the activation link does.
            user.MustConfirmEmail = false;

            token.MarkUsed(now);

            // After a password change every existing session is closed: whoever knew the old
            // password (or held a stolen refresh token) is signed out.
            await RevokeTokensAsync(user.Id, UserTokenType.RefreshToken, now, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync(user.Id, "Auth.ResetPassword", "User", user.Id, cancellationToken);
        }

        // ---------- helpers: account state ----------

        private static void EnsureCanSignIn(User user)
        {
            if (user.IsBlocked)
                throw new ForbiddenException("Ваш налог је блокиран. Обратите се администратору.");

            if (user.MustConfirmEmail)
                throw new ForbiddenException("Налог још није активиран. Отворите линк из e-mail поруке или затражите нови.");
        }

        // ---------- helpers: one-time tokens ----------

        /// <summary>
        /// Creates a token of the given type and returns the secret. Only its hash is stored;
        /// the secret itself exists only in the e-mail / response sent to the user.
        /// The caller saves the changes.
        /// </summary>
        private async Task<string> CreateTokenAsync(
            User user,
            UserTokenType type,
            TimeSpan lifetime,
            CancellationToken cancellationToken)
        {
            var secret = SecureToken.Generate();
            var now = DateTime.UtcNow;

            var token = new UserToken
            {
                UserId = user.Id,
                Type = type,
                TokenHash = SecureToken.Hash(secret),
                CreatedAt = now,
                ExpiresAt = now.Add(lifetime)
            };

            await _unitOfWork.UserTokens.AddAsync(token, cancellationToken);
            return secret;
        }

        private async Task<UserToken?> FindUsableTokenAsync(
            string secret,
            UserTokenType type,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var token = await _unitOfWork.UserTokens.GetByHashAsync(SecureToken.Hash(secret), type, cancellationToken);
            return token is not null && token.IsUsable(now) ? token : null;
        }

        /// <summary>Retires all still-usable tokens of the given type. The caller saves the changes.</summary>
        private async Task RevokeTokensAsync(
            Guid userId,
            UserTokenType type,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var tokens = await _unitOfWork.UserTokens.GetUsableByUserAsync(userId, type, now, cancellationToken);

            foreach (var token in tokens)
                token.MarkUsed(now);
        }

        // ---------- helpers: access and refresh tokens ----------

        private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
        {
            var refreshToken = await CreateTokenAsync(
                user, UserTokenType.RefreshToken,
                TimeSpan.FromDays(_settings.RefreshTokenDays), cancellationToken);

            return new AuthResponse
            {
                Id = user.Id,
                Username = user.Email,
                AccessToken = GenerateAccessToken(user),
                RefreshToken = refreshToken
            };
        }

        private string GenerateAccessToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        // ---------- helpers: e-mail ----------

        private Task SendActivationEmailAsync(User user, string token, CancellationToken cancellationToken)
        {
            var url = BuildFrontendUrl("confirm-email", token);
            var body =
                "Добро дошли у FairShare!\n\n" +
                "Да бисте активирали налог, отворите сљедећи линк:\n" +
                url + "\n\n" +
                $"Линк важи {_settings.EmailConfirmationHours} h. Ако нисте ви креирали налог, " +
                "занемарите ову поруку.";

            return SendEmailAsync(user, "Активација налога", body, url, cancellationToken);
        }

        /// <summary>Link to a page of the web application; that page then calls the API with the token.</summary>
        private string BuildFrontendUrl(string path, string token)
            => $"{_settings.FrontendBaseUrl.TrimEnd('/')}/{path}?token={Uri.EscapeDataString(token)}";

        private async Task SendEmailAsync(
            User user,
            string subject,
            string body,
            string url,
            CancellationToken cancellationToken)
        {
            // Queued and sent by the background service, so a slow mail server does not slow down the request.
            await _emailQueue.EnqueueAsync(
                new EmailMessage(user.Email, $"{user.FirstName} {user.LastName}", subject, body),
                cancellationToken);

            // Development convenience only: lets the flow be tested without a working mail server.
            // The link contains a secret, so it is never written to the log in other environments.
            if (_environment.IsDevelopment())
                _logger.LogInformation("[Development only] {Subject} link for {Email}: {Url}", subject, user.Email, url);
        }
    }
}
