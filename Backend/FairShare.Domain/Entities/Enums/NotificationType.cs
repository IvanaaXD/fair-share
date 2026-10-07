namespace FairShare.Domain.Entities.Enums;

public enum NotificationType
{
    AccountActivation,
    GroupInvite,
    PasswordReset,
    PaymentConfirmation,
    BudgetExceeded,

    // НОВО: додато на крај да се не помјере постојеће нумеричке вриједности у бази
    SettlementSuggested,
    SettlementCompleted
}
