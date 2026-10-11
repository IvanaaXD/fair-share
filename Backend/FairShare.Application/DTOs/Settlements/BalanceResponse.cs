namespace FairShare.Application.DTOs.Settlements;

public class BalanceResponse
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Positive = claims from the group, negative = owes the group.</summary>
    public decimal NetBalance { get; set; }
}
