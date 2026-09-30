namespace FairShare.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsSystemDefined { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<GroupExpense> GroupExpenses { get; set; } = new List<GroupExpense>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
