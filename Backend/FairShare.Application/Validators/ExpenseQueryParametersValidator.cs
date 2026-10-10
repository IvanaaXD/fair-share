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
                .GreaterThanOrEqualTo(1).WithMessage("Број странице мора бити најмање 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, ExpenseQueryParameters.MaxPageSize)
                .WithMessage($"Величина странице мора бити између 1 и {ExpenseQueryParameters.MaxPageSize}.");

            RuleFor(x => x.Search)
                .MaximumLength(ExpenseQueryParameters.MaxSearchLength)
                .WithMessage($"Текст претраге може имати највише {ExpenseQueryParameters.MaxSearchLength} карактера.");

            RuleFor(x => x.MinAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Најмањи износ не може бити негативан.")
                .When(x => x.MinAmount.HasValue);

            RuleFor(x => x.MaxAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Највећи износ не може бити негативан.")
                .When(x => x.MaxAmount.HasValue);

            RuleFor(x => x)
                .Must(x => x.MinAmount <= x.MaxAmount)
                .WithMessage("Најмањи износ не може бити већи од највећег.")
                .OverridePropertyName(nameof(ExpenseQueryParameters.MaxAmount))
                .When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue);

            RuleFor(x => x)
                .Must(x => x.From!.Value.Date <= x.To!.Value.Date)
                .WithMessage("Почетни датум не може бити послије крајњег.")
                .OverridePropertyName(nameof(ExpenseQueryParameters.To))
                .When(x => x.From.HasValue && x.To.HasValue);

            RuleFor(x => x.SortBy)
                .IsInEnum().WithMessage("Сортирање је могуће по датуму (Date) или износу (Amount).");
        }
    }
}
