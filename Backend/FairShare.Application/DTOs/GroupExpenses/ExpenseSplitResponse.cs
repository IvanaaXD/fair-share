namespace FairShare.Application.DTOs.GroupExpenses;

public class ExpenseSplitResponse
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double? Percentage { get; set; }
}
