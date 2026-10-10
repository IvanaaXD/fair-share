using FairShare.Application.DTOs.Expenses;
using FairShare.Domain.Entities.Enums;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
    {
        public CreateExpenseRequestValidator()
        {
            RuleFor(x => x.Amount).ValidMoneyAmount();
            RuleFor(x => x.Currency).ValidCurrency();
            RuleFor(x => x.Date).ValidExpenseDate();

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Категорија мора бити изабрана.");

            RuleFor(x => x.Description)
                .MaximumLength(ValidationRules.MaxDescriptionLength)
                .WithMessage($"Опис може имати највише {ValidationRules.MaxDescriptionLength} карактера.");

            // ReceiptImageUrl is no longer part of the request: the receipt is uploaded separately
            // (PUT /api/expenses/{id}/receipt) and the address is set by the server.

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0).WithMessage("Географска ширина мора бити између -90 и 90.")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0).WithMessage("Географска дужина мора бити између -180 и 180.")
                .When(x => x.Longitude.HasValue);

            // A location is a pair - one coordinate without the other is meaningless on a map.
            RuleFor(x => x)
                .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
                .WithMessage("Локација мора имати и географску ширину и дужину.")
                .OverridePropertyName("Location");

            RuleFor(x => x.RecurrenceInterval)
                .IsInEnum().WithMessage("Непознат интервал понављања.");

            RuleFor(x => x.RecurrenceInterval)
                .NotEqual(RecurrenceInterval.None)
                .WithMessage("За трошак који се понавља мора бити изабран интервал.")
                .When(x => x.IsRecurring);

            RuleFor(x => x.RecurrenceInterval)
                .Equal(RecurrenceInterval.None)
                .WithMessage("Интервал понављања се задаје само за трошак који се понавља.")
                .When(x => !x.IsRecurring);
        }
    }
}
