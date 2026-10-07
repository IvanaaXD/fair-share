namespace FairShare.Application.DTOs.Analytics;

public class ExpenseLocationResponse
{
    public Guid ExpenseId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
