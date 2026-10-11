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
                .NotEmpty().WithMessage("Currency is required.")
                .Matches("^[A-Z]{3}$").WithMessage("Currency must be a 3-letter uppercase code (e.g., BAM, EUR, RSD).");

        /// <summary>Positive money amount with at most two decimal places.</summary>
        public static IRuleBuilderOptions<T, decimal> ValidMoneyAmount<T>(this IRuleBuilder<T, decimal> rule)
            => rule
                .GreaterThan(0).WithMessage("Amount must be greater than zero.")
                .LessThanOrEqualTo(MaxMoneyAmount).WithMessage($"Amount cannot be greater than {MaxMoneyAmount:N0}.")
                .Must(HaveAtMostTwoDecimals).WithMessage("Amount can have at most two decimal places.");

        /// <summary>Expense date: not before 2000 and not in the future (one day of slack for time zones).</summary>
        public static IRuleBuilderOptions<T, DateTime> ValidExpenseDate<T>(this IRuleBuilder<T, DateTime> rule)
            => rule
                .NotEmpty().WithMessage("Date is required.")
                .Must(date => date >= MinExpenseDate).WithMessage("Date cannot be before the year 2000.")
                .Must(date => date <= DateTime.UtcNow.AddDays(1)).WithMessage("Date cannot be in the future.");

        public static bool HaveAtMostTwoDecimals(decimal value) => decimal.Round(value, 2) == value;
    }
}