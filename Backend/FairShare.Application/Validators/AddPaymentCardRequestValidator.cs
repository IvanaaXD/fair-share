using FairShare.Application.DTOs.Payments;
using FairShare.Domain.Services;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class AddPaymentCardRequestValidator : AbstractValidator<AddPaymentCardRequest>
    {
        public AddPaymentCardRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(x => x.CardNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Број картице је обавезан.")
                .Matches(@"^[\d\s-]+$").WithMessage("Број картице може садржати само цифре, размаке и цртице.")
                .Must(CardNumber.IsValid).WithMessage("Број картице није исправан.");

            RuleFor(x => x.ExpiryMonth)
                .InclusiveBetween(1, 12).WithMessage("Мјесец истека мора бити између 1 и 12.");

            RuleFor(x => x.ExpiryYear)
                .InclusiveBetween(2000, 2100).WithMessage("Година истека није исправна.");

            // A card is valid until the end of its expiry month.
            RuleFor(x => x)
                .Must(x => !IsExpired(x.ExpiryMonth, x.ExpiryYear, timeProvider.GetUtcNow().UtcDateTime))
                .WithMessage("Картица је истекла.")
                .OverridePropertyName(nameof(AddPaymentCardRequest.ExpiryYear))
                .When(x => x.ExpiryMonth is >= 1 and <= 12 && x.ExpiryYear is >= 2000 and <= 2100);

            RuleFor(x => x.Cvv)
                .Matches(@"^\d{3,4}$").WithMessage("CVV мора имати 3 или 4 цифре.");

            RuleFor(x => x.HolderName)
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Име на картици може имати највише {ValidationRules.MaxNameLength} карактера.");
        }

        public static bool IsExpired(int month, int year, DateTime now)
            => year < now.Year || (year == now.Year && month < now.Month);
    }
}
