using FairShare.Application.DTOs.Expenses;
using FairShare.Domain.Entities.Enums;
using FluentValidation;

namespace FairShare.Application.Validators
{
    /// <summary>Same rules as CreateExpenseRequestValidator - an updated expense must be just as valid.</summary>
    public class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
    {
        public UpdateExpenseRequestValidator()
        {
            RuleFor(x => x.Amount).ValidMoneyAmount();
            RuleFor(x => x.Currency).ValidCurrency();
            RuleFor(x => x.Date).ValidExpenseDate();

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category must be selected.");

            RuleFor(x => x.Description)
                .MaximumLength(ValidationRules.MaxDescriptionLength)
                .WithMessage($"Description can have at most {ValidationRules.MaxDescriptionLength} characters.");

            // ReceiptImageUrl is no longer part of the request: the receipt is uploaded separately
            // (PUT /api/expenses/{id}/receipt) and the address is set by the server.

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0).WithMessage("Latitude must be between -90 and 90.")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0).WithMessage("Longitude must be between -180 and 180.")
                .When(x => x.Longitude.HasValue);

            RuleFor(x => x)
                .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
                .WithMessage("Location must have both latitude and longitude.")
                .OverridePropertyName("Location");

            RuleFor(x => x.RecurrenceInterval)
                .IsInEnum().WithMessage("Unknown recurrence interval.");

            RuleFor(x => x.RecurrenceInterval)
                .NotEqual(RecurrenceInterval.None)
                .WithMessage("An interval must be selected for a recurring expense.")
                .When(x => x.IsRecurring);

            RuleFor(x => x.RecurrenceInterval)
                .Equal(RecurrenceInterval.None)
                .WithMessage("The recurrence interval is only specified for a recurring expense.")
                .When(x => !x.IsRecurring);
        }
    }
}