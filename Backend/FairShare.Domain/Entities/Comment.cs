namespace FairShare.Domain.Entities;

public class Comment : BaseEntity
{
    public string Text { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid GroupExpenseId { get; set; }
    public GroupExpense GroupExpense { get; set; } = null!;
}
