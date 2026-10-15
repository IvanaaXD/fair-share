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
                .NotEmpty().WithMessage("Category must be selected.");

            RuleFor(x => x.PaidByUserId)
                .NotEmpty().WithMessage("The member who paid the expense must be selected.");

            RuleFor(x => x.Description)
                .MaximumLength(ValidationRules.MaxDescriptionLength)
                .WithMessage($"Description can have at most {ValidationRules.MaxDescriptionLength} characters.");

            RuleFor(x => x.SplitType)
                .IsInEnum().WithMessage("Unknown split type.");

            // NEW: optional currency of the expense - a three-letter ISO code (EUR, BAM, RSD...).
            RuleFor(x => x.Currency)
                .Matches("^[A-Za-z]{3}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Currency))
                .WithMessage("Валута мора бити ознака од три слова, нпр. EUR.");

            RuleFor(x => x.Participants)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("The list of participants is required.")
                .Must(p => p.Count >= 2).WithMessage("A group expense must have at least two participants.")
                .Must(p => p.Select(x => x.UserId).Distinct().Count() == p.Count)
                .WithMessage("The list of participants contains duplicates.");

            RuleForEach(x => x.Participants).ChildRules(participant =>
            {
                participant.RuleFor(p => p.UserId)
                    .NotEmpty().WithMessage("Each participant must have a user.");
            });

            // Rules that depend on the chosen split model.
            When(x => x.SplitType == SplitType.Percentage && x.Participants != null, () =>
            {
                RuleForEach(x => x.Participants)
                    .Must(p => p.Percentage is > 0.0 and <= 100.0)
                    .WithMessage("Each participant must have a percentage greater than 0 and at most 100.");

                RuleFor(x => x.Participants)
                    .Must(p => Math.Abs(p.Sum(x => x.Percentage ?? 0) - 100.0) <= PercentageTolerance)
                    .WithMessage("The sum of percentages must be 100%.");
            });

            // Exact amounts are in the same currency as the total, so they are compared directly.
            When(x => x.SplitType == SplitType.ExactAmount && x.Participants != null, () =>
            {
                RuleForEach(x => x.Participants)
                    .Must(p => p.Amount is > 0m)
                    .WithMessage("Each participant must have an exact amount greater than zero.");

                RuleFor(x => x)
                    .Must(x => Math.Abs(x.Participants.Sum(p => p.Amount ?? 0m) - x.Amount) <= AmountTolerance)
                    .WithMessage("The sum of exact amounts must be equal to the total expense amount.")
                    .OverridePropertyName(nameof(CreateGroupExpenseRequest.Participants));
            });
        }
    }
}
