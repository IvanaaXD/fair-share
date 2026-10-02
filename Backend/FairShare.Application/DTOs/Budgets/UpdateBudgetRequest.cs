namespace FairShare.Application.DTOs.Budgets;

/// <summary>Мијења се само лимит - категорија и мјесец су фиксни за постојећи буџет (креирај нов за други мјесец).</summary>
public class UpdateBudgetRequest
{
    public decimal MonthlyLimit { get; set; }
}
