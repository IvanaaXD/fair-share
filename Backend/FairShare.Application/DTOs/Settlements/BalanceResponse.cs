namespace FairShare.Application.DTOs.Settlements;

public class BalanceResponse
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Позитивно = потражује од групе, негативно = дугује групи.</summary>
    public decimal NetBalance { get; set; }
}
