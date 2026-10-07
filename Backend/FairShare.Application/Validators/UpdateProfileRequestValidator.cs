using FairShare.Application.DTOs.Profile;
using FairShare.Domain.Services;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Име је обавезно.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Име може имати највише {ValidationRules.MaxNameLength} карактера.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Презиме је обавезно.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Презиме може имати највише {ValidationRules.MaxNameLength} карактера.");

            RuleFor(x => x.DefaultCurrency).ValidCurrency();

            // The account may be typed with dashes or spaces ("161-0000012345678-90");
            // ProfileService stores only the digits.
            When(x => !string.IsNullOrWhiteSpace(x.BankAccountNumber), () =>
            {
                RuleFor(x => x.BankAccountNumber!)
                    .Matches(@"^[\d\s-]+$").WithMessage("Број рачуна може садржати само цифре, размаке и цртице.")
                    .Must(account => IpsQrCodec.IsValidAccount(IpsQrCodec.NormalizeAccount(account)))
                    .WithMessage("Број рачуна мора имати 16 или 18 цифара.");
            });
        }
    }
}
