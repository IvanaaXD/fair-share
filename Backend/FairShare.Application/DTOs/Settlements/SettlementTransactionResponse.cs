using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Settlements;

public class SettlementTransactionResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid DebtorUserId { get; set; }
    public string DebtorName { get; set; } = string.Empty;
    public Guid CreditorUserId { get; set; }
    public string CreditorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public SettlementStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // When the creditor last sent a reminder for this debt (UTC); null if never.
    // The app uses it to show "reminder sent" and when the next one is possible.
    public DateTime? LastReminderSentAt { get; set; }

    public QrPaymentDataResponse? QrPaymentData { get; set; }
}
