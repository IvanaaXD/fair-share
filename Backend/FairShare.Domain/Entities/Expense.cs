using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

public class Expense : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BAM";
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ReceiptImageUrl { get; set; }

    public bool IsRecurring { get; set; }
    public RecurrenceInterval RecurrenceInterval { get; set; } = RecurrenceInterval.None;
}
