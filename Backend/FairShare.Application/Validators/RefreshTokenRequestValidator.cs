using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.")
                .MaximumLength(200).WithMessage("Refresh token is not valid.");
        }
    }
}