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

    // Веза "1" -- "1" : QR подаци се генеришу за сваку трансакцију поравнања
    public QrPaymentData? QrPaymentData { get; set; }

    // Веза "1" -- "0..1" : поравнање не мора увијек бити измирено картицом
    public Payment? Payment { get; set; }

    /// <summary>
    /// Генерише (или освјежава) QR податке за уплату. Рачун примаоца се узима из
    /// профила повјериоца; ако га још није унио, остаје празан и QR слика се неће
    /// моћи генерисати док га не унесе.
    /// </summary>
    public QrPaymentData GenerateQrCode()
    {
        // ИЗМЈЕНА: умјесто CreditorUserId користи се стварни број рачуна повјериоца
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
        }

        return QrPaymentData;
    }
}
