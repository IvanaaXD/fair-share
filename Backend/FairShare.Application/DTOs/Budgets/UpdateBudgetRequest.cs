namespace FairShare.Application.DTOs.Budgets;

/// <summary>Only the limit is changed - the category and month are fixed for an existing budget (create a new one for a different month).</summary>
public class UpdateBudgetRequest
{
    public decimal MonthlyLimit { get; set; }
}
