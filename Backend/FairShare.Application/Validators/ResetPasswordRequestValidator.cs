using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Токен за промјену лозинке је обавезан.")
                .MaximumLength(200).WithMessage("Токен за промјену лозинке није исправан.");

            RuleFor(x => x.NewPassword).ValidNewPassword();
        }
    }
}
