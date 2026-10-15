namespace FairShare.Application.Common.Exceptions;

/// <summary>
/// Codes of errors the user can run into, returned as "code" next to the message. They never
/// change once the apps use them - the web and mobile apps keep the translations.
/// </summary>
public static class ErrorCodes
{
    // ---------- general ----------
    public const string DataChanged = "dataChanged";
    public const string DataConflict = "dataConflict";
    public const string ServerError = "serverError";
    public const string CategoryNotFound = "categoryNotFound";
    public const string UserNotFound = "userNotFound";

    // ---------- groups ----------
    public const string GroupNotFound = "groupNotFound";
    public const string NotGroupMember = "notGroupMember";
    public const string GroupOwnerOnly = "groupOwnerOnly";
    public const string GroupHasDebts = "groupHasDebts";
    public const string UserEmailNotFound = "userEmailNotFound";
    public const string AlreadyGroupMember = "alreadyGroupMember";
    public const string CannotRemoveSelf = "cannotRemoveSelf";
    public const string MemberHasBalance = "memberHasBalance";
    public const string OnlyMemberCannotLeave = "onlyMemberCannotLeave";
    public const string LeaveWithBalance = "leaveWithBalance";

    // ---------- group expenses ----------
    public const string GroupExpenseNotFound = "groupExpenseNotFound";
    public const string GroupExpenseModifyForbidden = "groupExpenseModifyForbidden";
    public const string GroupExpenseFormerMember = "groupExpenseFormerMember";
    public const string PayerNotMember = "payerNotMember";
    public const string ParticipantNotMember = "participantNotMember";

    // ---------- settlements and payments ----------
    public const string SettlementNotFound = "settlementNotFound";
    public const string SettlementNotParticipant = "settlementNotParticipant";
    public const string SettlementAlreadySettled = "settlementAlreadySettled";
    public const string FormerMemberBalance = "formerMemberBalance";
    public const string PaymentKeyReused = "paymentKeyReused";
    public const string PaymentOnlyDebtor = "paymentOnlyDebtor";
    public const string CardNotFound = "cardNotFound";
    public const string CardExpired = "cardExpired";
    public const string PaymentAlreadyPaid = "paymentAlreadyPaid";
    public const string PaymentInProgress = "paymentInProgress";
    public const string ReminderOnlyCreditor = "reminderOnlyCreditor";
    public const string ReminderTooSoon = "reminderTooSoon";

    // ---------- budgets, notifications ----------
    public const string BudgetExists = "budgetExists";
    public const string BudgetNotFound = "budgetNotFound";
    public const string InvalidMonth = "invalidMonth";
    public const string NotificationNotFound = "notificationNotFound";

    // ---------- administration ----------
    public const string CannotBlockSelf = "cannotBlockSelf";

    // ---------- analytics ----------
    public const string AnalyticsRangeRequired = "analyticsRangeRequired";
    public const string AnalyticsRangeOrder = "analyticsRangeOrder";
    public const string AnalyticsRangeTooLong = "analyticsRangeTooLong";
}
