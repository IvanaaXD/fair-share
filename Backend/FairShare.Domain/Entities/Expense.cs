using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Services;

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

    // NEW: when the next automatic copy of this expense is due; null if it does not repeat.
    // A recurring expense works as a template: the background job creates ordinary
    // (non-recurring) copies of it, and this date moves forward after every copy.
    public DateTime? NextOccurrenceDate { get; set; }

    /// <summary>
    /// Sets when the first automatic copy is due. Called when the expense is created and when its
    /// date or repetition changes. Missed past occurrences are NOT added: a monthly expense entered
    /// today with a date three months ago gets its next copy next month, not three copies at once.
    /// </summary>
    public void ScheduleNextOccurrence(DateTime nowUtc)
    {
        if (!IsRecurring || RecurrenceInterval == RecurrenceInterval.None)
        {
            NextOccurrenceDate = null;
            return;
        }

        var after = Date > nowUtc ? Date : nowUtc;
        NextOccurrenceDate = RecurrenceSchedule.NextOccurrenceAfter(Date, RecurrenceInterval, after);
    }

    public bool IsOccurrenceDue(DateTime nowUtc)
        => IsRecurring && NextOccurrenceDate is { } next && next <= nowUtc;

    /// <summary>
    /// Creates the copy that is due now and moves <see cref="NextOccurrenceDate"/> to the one
    /// after it. The copy is an ordinary expense: it does not repeat itself and has no receipt.
    /// </summary>
    public Expense CreateNextOccurrence()
    {
        if (NextOccurrenceDate is not { } occurrenceDate)
            throw new InvalidOperationException("No occurrence is scheduled for this expense.");

        var occurrence = new Expense
        {
            UserId = UserId,
            CategoryId = CategoryId,
            Amount = Amount,
            Currency = Currency,
            Date = occurrenceDate,
            Description = Description,
            Latitude = Latitude,
            Longitude = Longitude,
            IsRecurring = false,
            RecurrenceInterval = RecurrenceInterval.None
        };

        NextOccurrenceDate = RecurrenceSchedule.NextOccurrenceAfter(Date, RecurrenceInterval, occurrenceDate);
        return occurrence;
    }
}
