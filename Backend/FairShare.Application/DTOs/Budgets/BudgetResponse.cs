namespace FairShare.Application.DTOs.Budgets;

public class BudgetResponse
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public string Month { get; set; } = string.Empty;
    public decimal CurrentSpending { get; set; }
    public double PercentageUsed { get; set; }
}
