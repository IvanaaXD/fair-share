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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // NEW: optimistic concurrency token. Npgsql maps it to PostgreSQL's built-in "xmin" system
    // column, which changes on every update of the row. If two requests try to settle (or
    // delete) the same transaction at the same time, the second one fails instead of
    // silently overwriting the first.
    public uint Version { get; set; }

    // One-to-one: QR data is generated for every settlement transaction.
    public QrPaymentData? QrPaymentData { get; set; }

    // One-to-zero-or-one: a settlement does not have to be paid by card.
    public Payment? Payment { get; set; }

    /// <summary>
    /// Creates (or refreshes) the QR payment data. The recipient account is taken from the
    /// creditor's profile; if they have not entered one yet it stays empty, and the QR image
    /// cannot be generated until they do.
    /// </summary>
    public QrPaymentData GenerateQrCode()
    {
        var account = CreditorUser?.BankAccountNumber ?? string.Empty;

        if (QrPaymentData is null)
        {
            QrPaymentData = new QrPaymentData
            {
                SettlementTransactionId = Id,
                RecipientAccount = account,
                Amount = Amount,
                Currency = Group.Currency,
                ReferenceCode = Id.ToString("N")
            };
        }
        else
        {
            QrPaymentData.RecipientAccount = account;
            QrPaymentData.Amount = Amount;
            QrPaymentData.ReferenceCode = Id.ToString("N");
        }

        return QrPaymentData;
    }
}
