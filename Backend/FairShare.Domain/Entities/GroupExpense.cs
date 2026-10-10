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

    public decimal Amount { get; set; }
    public string? Description { get; set; }
    // Address of the receipt photo; set only by ReceiptService after an upload.
    public string? ReceiptImageUrl { get; set; }
    public DateTime Date { get; set; }
    public SplitType SplitType { get; set; }

    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    /// <summary>
    /// Рачуна појединачне износе у <see cref="Splits"/> на основу <see cref="SplitType"/>-а.
    /// Конкретна логика (једнако / процентуално / тачни износи) имплементира се
    /// у апликационом слоју (domain service), ова метода служи као улазна тачка домена.
    /// </summary>
    public void CalculateSplits()
    {
        // TODO: имплементирати у апликационом/доменском сервису
    }
}
