namespace FairShare.Domain.Entities;

public class ExpenseSplit : BaseEntity
{
    public Guid GroupExpenseId { get; set; }
    public GroupExpense GroupExpense { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal Amount { get; set; }

    // Попуњава се само када је GroupExpense.SplitType == Percentage
    public double? Percentage { get; set; }
}
