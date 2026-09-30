using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

public class SettlementTransaction : BaseEntity
{
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public Guid DebtorUserId { get; set; }
    public User DebtorUser { get; set; } = null!;

    public Guid CreditorUserId { get; set; }
    public User CreditorUser { get; set; } = null!;

    public decimal Amount { get; set; }
    public SettlementStatus Status { get; set; } = SettlementStatus.Proposed;

    public QrPaymentData? QrPaymentData { get; set; }

    public Payment? Payment { get; set; }

    public QrPaymentData GenerateQrCode()
    {
        QrPaymentData = new QrPaymentData
        {
            SettlementTransactionId = Id,
            RecipientAccount = CreditorUserId.ToString(), // TODO: заменити стварним рачуном примаоца
            Amount = Amount,
            Currency = Group.Currency,
            ReferenceCode = Id.ToString("N")
        };
        return QrPaymentData;
    }
}
