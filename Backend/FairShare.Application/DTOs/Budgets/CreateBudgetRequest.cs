namespace FairShare.Application.DTOs.Budgets;

public class CreateBudgetRequest
{
    public Guid CategoryId { get; set; }
    public decimal MonthlyLimit { get; set; }

    /// <summary>Формат "ГГГГ-ММ", нпр. "2026-09".</summary>
    public string Month { get; set; } = string.Empty;
}
