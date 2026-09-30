namespace FairShare.Domain.Entities;

public class QrPaymentData : BaseEntity
{
    public Guid SettlementTransactionId { get; set; }
    public SettlementTransaction SettlementTransaction { get; set; } = null!;

    public string RecipientAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BAM";
    public string ReferenceCode { get; set; } = string.Empty;
}
