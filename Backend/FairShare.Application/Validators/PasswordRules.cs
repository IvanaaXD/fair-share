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
                .NotEmpty().WithMessage("Лозинка је обавезна.")
                .MinimumLength(MinLength).WithMessage($"Лозинка мора имати најмање {MinLength} карактера.")
                .MaximumLength(MaxLength).WithMessage($"Лозинка може имати највише {MaxLength} карактера.")
                .Matches(@"\p{L}").WithMessage("Лозинка мора садржати бар једно слово.")
                .Matches(@"\d").WithMessage("Лозинка мора садржати бар једну цифру.");
    }
}
