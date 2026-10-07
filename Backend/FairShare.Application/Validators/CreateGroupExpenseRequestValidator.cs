using FairShare.Application.DTOs.GroupExpenses;
using FairShare.Domain.Entities.Enums;
using FluentValidation;

namespace FairShare.Application.Validators
{
    /// <summary>
    /// Checks the shape of a group expense before the service touches the database.
    /// ExpenseSplitCalculator re-checks the split rules in the domain (defense in depth),
    /// and the service checks what only the database knows (group membership).
    /// </summary>
    public class CreateGroupExpenseRequestValidator : AbstractValidator<CreateGroupExpenseRequest>
    {
        private const double PercentageTolerance = 0.01;
        private const decimal AmountTolerance = 0.01m;

        public CreateGroupExpenseRequestValidator()
        {
            RuleFor(x => x.Amount).ValidMoneyAmount();
            RuleFor(x => x.Date).ValidExpenseDate();

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Категорија мора бити изабрана.");

            RuleFor(x => x.PaidByUserId)
                .NotEmpty().WithMessage("Мора бити изабран члан који је платио трошак.");

            RuleFor(x => x.Description)
                .MaximumLength(ValidationRules.MaxDescriptionLength)
                .WithMessage($"Опис може имати највише {ValidationRules.MaxDescriptionLength} карактера.");

            RuleFor(x => x.SplitType)
                .IsInEnum().WithMessage("Непознат модел подјеле трошка.");

            RuleFor(x => x.Participants)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Листа учесника је обавезна.")
                .Must(p => p.Count >= 2).WithMessage("Групни трошак мора имати најмање два учесника.")
                .Must(p => p.Select(x => x.UserId).Distinct().Count() == p.Count)
                .WithMessage("Листа учесника садржи дупликате.");

            RuleForEach(x => x.Participants).ChildRules(participant =>
            {
                participant.RuleFor(p => p.UserId)
                    .NotEmpty().WithMessage("Сваки учесник мора имати корисника.");
            });

            // Rules that depend on the chosen split model.
            When(x => x.SplitType == SplitType.Percentage && x.Participants != null, () =>
            {
                RuleForEach(x => x.Participants)
                    .Must(p => p.Percentage is > 0.0 and <= 100.0)
                    .WithMessage("Сваки учесник мора имати проценат већи од 0 и највише 100.");

                RuleFor(x => x.Participants)
                    .Must(p => Math.Abs(p.Sum(x => x.Percentage ?? 0) - 100.0) <= PercentageTolerance)
                    .WithMessage("Збир процената мора бити 100%.");
            });

            When(x => x.SplitType == SplitType.ExactAmount && x.Participants != null, () =>
            {
                RuleForEach(x => x.Participants)
                    .Must(p => p.Amount is > 0m)
                    .WithMessage("Сваки учесник мора имати тачан износ већи од нуле.");

                RuleFor(x => x)
                    .Must(x => Math.Abs(x.Participants.Sum(p => p.Amount ?? 0m) - x.Amount) <= AmountTolerance)
                    .WithMessage("Збир тачних износа мора бити једнак укупном износу трошка.")
                    .OverridePropertyName(nameof(CreateGroupExpenseRequest.Participants));
            });
        }
    }
}
