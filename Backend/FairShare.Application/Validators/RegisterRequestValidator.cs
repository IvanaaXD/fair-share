using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"First name can have at most {ValidationRules.MaxNameLength} characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Last name can have at most {ValidationRules.MaxNameLength} characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail address is required.")
                .EmailAddress().WithMessage("E-mail address is not valid.")
                .MaximumLength(256).WithMessage("E-mail address can have at most 256 characters.");

            RuleFor(x => x.Password).ValidNewPassword();

            RuleFor(x => x.DefaultCurrency).ValidCurrency();
        }
    }
}