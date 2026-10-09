namespace FairShare.Domain.Entities.Enums;

public enum UserTokenType
{
    /// <summary>Sent by e-mail after registration; activates the account.</summary>
    EmailConfirmation,

    /// <summary>Sent by e-mail on "forgot password"; allows setting a new password.</summary>
    PasswordReset,

    /// <summary>Long-lived token the client exchanges for a new access token.</summary>
    RefreshToken
}
