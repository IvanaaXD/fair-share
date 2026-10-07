using FluentValidation;

namespace FairShare.Application.Validators
{
    /// <summary>
    /// Reusable rules shared by several validators, so that the same field is validated
    /// the same way everywhere. Limits match the column lengths in Data/Configurations.
    /// </summary>
    public static class ValidationRules
    {
        public const int MaxNameLength = 100;
        public const int MaxDescriptionLength = 500;
        public const int MaxUrlLength = 500;

        /// <summary>Upper bound that comfortably fits the decimal(18,2) money columns.</summary>
        public const decimal MaxMoneyAmount = 1_000_000_000m;

        private static readonly DateTime MinExpenseDate = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>ISO 4217 currency code: exactly three upper-case letters.</summary>
        public static IRuleBuilderOptions<T, string> ValidCurrency<T>(this IRuleBuilder<T, string> rule)
            => rule
                .NotEmpty().WithMessage("Валута је обавезна.")
                .Matches("^[A-Z]{3}$").WithMessage("Валута мора бити ознака од 3 велика слова (нпр. BAM, EUR, RSD).");

        /// <summary>Positive money amount with at most two decimal places.</summary>
        public static IRuleBuilderOptions<T, decimal> ValidMoneyAmount<T>(this IRuleBuilder<T, decimal> rule)
            => rule
                .GreaterThan(0).WithMessage("Износ мора бити већи од нуле.")
                .LessThanOrEqualTo(MaxMoneyAmount).WithMessage($"Износ не може бити већи од {MaxMoneyAmount:N0}.")
                .Must(HaveAtMostTwoDecimals).WithMessage("Износ може имати највише двије децимале.");

        /// <summary>Expense date: not before 2000 and not in the future (one day of slack for time zones).</summary>
        public static IRuleBuilderOptions<T, DateTime> ValidExpenseDate<T>(this IRuleBuilder<T, DateTime> rule)
            => rule
                .NotEmpty().WithMessage("Датум је обавезан.")
                .Must(date => date >= MinExpenseDate).WithMessage("Датум не може бити прије 2000. године.")
                .Must(date => date <= DateTime.UtcNow.AddDays(1)).WithMessage("Датум не може бити у будућности.");

        public static bool HaveAtMostTwoDecimals(decimal value) => decimal.Round(value, 2) == value;
    }
}
