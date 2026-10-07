using FairShare.Application.DTOs.Budgets;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
    {
        public CreateBudgetRequestValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Категорија мора бити изабрана.");

            RuleFor(x => x.MonthlyLimit).ValidMoneyAmount();

            RuleFor(x => x.Month)
                .NotEmpty().WithMessage("Мјесец је обавезан.")
                .Matches(@"^\d{4}-(0[1-9]|1[0-2])$").WithMessage("Формат мјесеца мора бити ГГГГ-ММ (нпр. 2026-09).");
        }
    }
}
