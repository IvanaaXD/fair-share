using FairShare.Application.DTOs.Expenses;
using FluentValidation;

namespace FairShare.Application.Validators
{
    /// <summary>Runs automatically (ValidationFilter) for the query string of GET /api/expenses.</summary>
    public class ExpenseQueryParametersValidator : AbstractValidator<ExpenseQueryParameters>
    {
        public ExpenseQueryParametersValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, ExpenseQueryParameters.MaxPageSize)
                .WithMessage($"Page size must be between 1 and {ExpenseQueryParameters.MaxPageSize}.");

            RuleFor(x => x.Search)
                .MaximumLength(ExpenseQueryParameters.MaxSearchLength)
                .WithMessage($"Search text can have at most {ExpenseQueryParameters.MaxSearchLength} characters.");

            RuleFor(x => x.MinAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Minimum amount cannot be negative.")
                .When(x => x.MinAmount.HasValue);

            RuleFor(x => x.MaxAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Maximum amount cannot be negative.")
                .When(x => x.MaxAmount.HasValue);

            RuleFor(x => x)
                .Must(x => x.MinAmount <= x.MaxAmount)
                .WithMessage("Minimum amount cannot be greater than the maximum amount.")
                .OverridePropertyName(nameof(ExpenseQueryParameters.MaxAmount))
                .When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue);

            RuleFor(x => x)
                .Must(x => x.From!.Value.Date <= x.To!.Value.Date)
                .WithMessage("Start date cannot be after the end date.")
                .OverridePropertyName(nameof(ExpenseQueryParameters.To))
                .When(x => x.From.HasValue && x.To.HasValue);

            RuleFor(x => x.SortBy)
                .IsInEnum().WithMessage("Sorting is possible by date (Date) or amount (Amount).");
        }
    }
}