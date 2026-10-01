using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.GroupExpenses;

public class GroupExpenseResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid PaidByUserId { get; set; }
    public string PaidByName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public SplitType SplitType { get; set; }
    public List<ExpenseSplitResponse> Splits { get; set; } = new();
}
