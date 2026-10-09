using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
    {
        public ConfirmEmailRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Токен за активацију је обавезан.")
                .MaximumLength(200).WithMessage("Токен за активацију није исправан.");
        }
    }
}
