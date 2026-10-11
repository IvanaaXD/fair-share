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
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"First name can have at most {ValidationRules.MaxNameLength} characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Last name can have at most {ValidationRules.MaxNameLength} characters.");

            RuleFor(x => x.DefaultCurrency).ValidCurrency();

            // The account may be typed with dashes or spaces ("161-0000012345678-90");
            // ProfileService stores only the digits.
            When(x => !string.IsNullOrWhiteSpace(x.BankAccountNumber), () =>
            {
                RuleFor(x => x.BankAccountNumber!)
                    .Matches(@"^[\d\s-]+$").WithMessage("Account number can only contain digits, spaces, and dashes.")
                    .Must(account => IpsQrCodec.IsValidAccount(IpsQrCodec.NormalizeAccount(account)))
                    .WithMessage("Account number must have 16 or 18 digits.");
            });
        }
    }
}