using FairShare.Application.DTOs.Budgets;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
    {
        public CreateBudgetRequestValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category must be selected.");

            RuleFor(x => x.MonthlyLimit).ValidMoneyAmount();

            RuleFor(x => x.Month)
                .NotEmpty().WithMessage("Month is required.")
                .Matches(@"^\d{4}-(0[1-9]|1[0-2])$").WithMessage("Month format must be YYYY-MM (e.g., 2026-09).");
        }
    }
}