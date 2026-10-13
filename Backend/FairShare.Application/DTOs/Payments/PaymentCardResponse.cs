namespace FairShare.Application.DTOs.Payments;

public class PaymentCardResponse
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsExpired { get; set; }
}
