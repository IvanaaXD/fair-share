namespace FairShare.Application.DTOs.Settlements;

public class QrPaymentDataResponse
{
    public string RecipientAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ReferenceCode { get; set; } = string.Empty;
}
