namespace FairShare.Application.DTOs.Budgets;

public class CreateBudgetRequest
{
    public Guid CategoryId { get; set; }
    public decimal MonthlyLimit { get; set; }

    /// <summary>Format "YYYY-MM", e.g., "2026-09".</summary>
    public string Month { get; set; } = string.Empty;
}
