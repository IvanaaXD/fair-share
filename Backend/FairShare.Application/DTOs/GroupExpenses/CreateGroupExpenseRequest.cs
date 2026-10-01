using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.GroupExpenses;

public class CreateGroupExpenseRequest
{
    public Guid CategoryId { get; set; }
    public Guid PaidByUserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public SplitType SplitType { get; set; }
    public List<SplitParticipantRequest> Participants { get; set; } = new();
}
