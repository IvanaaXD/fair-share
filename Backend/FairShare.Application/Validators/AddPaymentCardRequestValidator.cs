using FairShare.Application.DTOs.Payments;
using FairShare.Domain.Services;
using FluentValidation;

namespace FairShare.Application.Validators;

public class AddPaymentCardRequestValidator : AbstractValidator<AddPaymentCardRequest>
{
    public AddPaymentCardRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.CardNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Card number is required.")
            .Matches(@"^[\d\s-]+$").WithMessage("Card number can only contain digits, spaces, and hyphens.")
            .Must(CardNumber.IsValid).WithMessage("Card number is not valid.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12).WithMessage("Expiry month must be between 1 and 12.");

        RuleFor(x => x.ExpiryYear)
            .InclusiveBetween(2000, 2100).WithMessage("Expiry year is not valid.");

        // A card is valid until the end of its expiry month.
        RuleFor(x => x)
            .Must(x => !IsExpired(x.ExpiryMonth, x.ExpiryYear, timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("Card has expired.")
            .OverridePropertyName(nameof(AddPaymentCardRequest.ExpiryYear))
            .When(x => x.ExpiryMonth is >= 1 and <= 12 && x.ExpiryYear is >= 2000 and <= 2100);

        RuleFor(x => x.Cvv)
            .Matches(@"^\d{3,4}$").WithMessage("CVV must be 3 or 4 digits.");

        RuleFor(x => x.HolderName)
            .MaximumLength(ValidationRules.MaxNameLength)
            .WithMessage($"Cardholder name can have at most {ValidationRules.MaxNameLength} characters.");
    }

    public static bool IsExpired(int month, int year, DateTime now)
        => year < now.Year || (year == now.Year && month < now.Month);
}