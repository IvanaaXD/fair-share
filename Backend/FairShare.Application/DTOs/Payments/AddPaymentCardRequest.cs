namespace FairShare.Application.DTOs.Payments;

/// <summary>
/// A new card. The number and CVV go to the payment simulator, which returns a token;
/// only the token, the last four digits, the brand and the expiry date are saved.
/// </summary>
public class AddPaymentCardRequest
{
    /// <summary>Spaces and dashes are allowed ("4242 4242 4242 4242").</summary>
    public string CardNumber { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    /// <summary>Four digits, e.g. 2028.</summary>
    public int ExpiryYear { get; set; }
    public string Cvv { get; set; } = string.Empty;
    public string? HolderName { get; set; }
}
