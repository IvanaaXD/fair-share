using FluentValidation;

namespace FairShare.Application.Validators
{
    /// <summary>
    /// The password policy, written once and used wherever a new password is chosen
    /// (registration, password reset, password change).
    /// </summary>
    public static class PasswordRules
    {
        public const int MinLength = 8;

        /// <summary>bcrypt only takes the first 72 bytes of a password into account, so longer ones are rejected.</summary>
        public const int MaxLength = 72;

        public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> rule)
            => rule
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(MinLength).WithMessage($"Password must have at least {MinLength} characters.")
                .MaximumLength(MaxLength).WithMessage($"Password can have at most {MaxLength} characters.")
                .Matches(@"\p{L}").WithMessage("Password must contain at least one letter.")
                .Matches(@"\d").WithMessage("Password must contain at least one digit.");
    }
}