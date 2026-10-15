using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

public class GroupExpense : BaseEntity
{
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid PaidByUserId { get; set; }
    public User PaidByUser { get; set; } = null!;

    /// <summary>
    /// Amount in the GROUP's currency. Balances, splits, settlements, QR codes and card payments
    /// all use only this amount, so a group always has exactly one currency to settle in.
    /// </summary>
    public decimal Amount { get; set; }

    public string? Description { get; set; }
    // Address of the receipt photo; set only by ReceiptService after an upload.
    public string? ReceiptImageUrl { get; set; }
    public DateTime Date { get; set; }
    public SplitType SplitType { get; set; }

    // NEW: an expense paid in another currency (e.g. 20 EUR in a group that keeps BAM).
    // It is converted once, when it is entered, and the rate is kept with it - so balances
    // do not change from day to day with the exchange rate. All four are null for an expense
    // in the group's own currency.
    public string? OriginalCurrency { get; set; }
    public decimal? OriginalAmount { get; set; }

    /// <summary>Units of the group's currency for one unit of the original currency.</summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>When the rate was published; null for the fixed EUR/BAM rate.</summary>
    public DateTime? ExchangeRateDate { get; set; }

    public bool IsConverted => OriginalCurrency is not null;

    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
