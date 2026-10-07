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

    // NEW: removed from a group, or became the group owner. Added at the end so the numeric
    // values already stored in the database do not shift.
    GroupMembershipChanged
}
