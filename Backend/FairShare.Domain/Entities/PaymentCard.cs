namespace FairShare.Domain.Entities;

public class PaymentCard : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Tokenized card data - never the full card number or CVV
    public string Token { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
