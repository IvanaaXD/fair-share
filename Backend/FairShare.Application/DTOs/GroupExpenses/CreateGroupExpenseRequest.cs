using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.GroupExpenses;

public class CreateGroupExpenseRequest
{
    public Guid CategoryId { get; set; }
    public Guid PaidByUserId { get; set; }

    /// <summary>In <see cref="Currency"/>; exact split amounts are in the same currency.</summary>
    public decimal Amount { get; set; }

    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public SplitType SplitType { get; set; }
    public List<SplitParticipantRequest> Participants { get; set; } = new();

    /// <summary>
    /// currency in which the expense was paid (ISO code, e.g. "EUR"). Empty or the group's
    /// own currency means no conversion; any other currency is converted to the group's currency.
    /// </summary>
    public string? Currency { get; set; }
}
