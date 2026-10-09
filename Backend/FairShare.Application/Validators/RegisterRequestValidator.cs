using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Име је обавезно.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Име може имати највише {ValidationRules.MaxNameLength} карактера.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Презиме је обавезно.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Презиме може имати највише {ValidationRules.MaxNameLength} карактера.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail адреса је обавезна.")
                .EmailAddress().WithMessage("E-mail адреса није исправна.")
                .MaximumLength(256).WithMessage("E-mail адреса може имати највише 256 карактера.");

            RuleFor(x => x.Password).ValidNewPassword();

            RuleFor(x => x.DefaultCurrency).ValidCurrency();
        }
    }
}
