namespace FairShare.Application.DTOs.GroupExpenses;

/// <summary>Split participant - Amount/Percentage are populated depending on the request's SplitType.</summary>
public class SplitParticipantRequest
{
    public Guid UserId { get; set; }
    public decimal? Amount { get; set; }
    public double? Percentage { get; set; }
}
