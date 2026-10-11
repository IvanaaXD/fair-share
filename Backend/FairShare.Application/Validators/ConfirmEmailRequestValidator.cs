using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
    {
        public ConfirmEmailRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Activation token is required.")
                .MaximumLength(200).WithMessage("Activation token is not valid.");
        }
    }
}