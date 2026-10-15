namespace FairShare.Domain.Entities.Enums;

public enum NotificationType
{
    AccountActivation,
    GroupInvite,
    PasswordReset,
    PaymentConfirmation,
    BudgetExceeded,
    SettlementSuggested,
    SettlementCompleted,

    // Removed from a group, or became the group owner. Added at the end so the numeric
    // values already stored in the database do not shift.
    GroupMembershipChanged,

    // a copy of a recurring expense was added automatically. Also at the end.
    RecurringExpenseCreated,

    // Another member added a group expense in which this user takes part. Also at the end.
    GroupExpenseAdded,

    // A member this user owes money to sent a reminder. Also at the end.
    DebtReminder
}
