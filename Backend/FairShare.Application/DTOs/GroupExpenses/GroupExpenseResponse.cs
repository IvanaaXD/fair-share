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
    public string? ReceiptImageUrl { get; set; }
    public DateTime Date { get; set; }
    public SplitType SplitType { get; set; }
    public List<ExpenseSplitResponse> Splits { get; set; } = new();

    // Number of comments, shown next to the expense in the list.
    public int CommentCount { get; set; }

    // An expense paid in another currency - what was paid and the rate it was converted
    // with. Amount and Splits are always in the group's currency. Null when not converted.
    public string? OriginalCurrency { get; set; }
    public decimal? OriginalAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public DateTime? ExchangeRateDate { get; set; }
}
