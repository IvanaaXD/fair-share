using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Password reset token is required.")
                .MaximumLength(200).WithMessage("Password reset token is not valid.");

            RuleFor(x => x.NewPassword).ValidNewPassword();
        }
    }
}