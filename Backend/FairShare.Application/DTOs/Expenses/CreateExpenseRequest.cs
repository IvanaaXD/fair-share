using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Expenses;

public class CreateExpenseRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BAM";
    public DateTime Date { get; set; }
    public Guid CategoryId { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ReceiptImageUrl { get; set; }
    public bool IsRecurring { get; set; }
    public RecurrenceInterval RecurrenceInterval { get; set; } = RecurrenceInterval.None;
}
