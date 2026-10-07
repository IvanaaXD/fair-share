using FairShare.Application.DTOs.Budgets;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
    {
        public UpdateBudgetRequestValidator()
        {
            RuleFor(x => x.MonthlyLimit).ValidMoneyAmount();
        }
    }
}
