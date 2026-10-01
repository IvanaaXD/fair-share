namespace FairShare.Application.DTOs.GroupExpenses;

/// <summary>Учесник подјеле - Amount/Percentage се попуњавају у зависности од SplitType-а захтјева.</summary>
public class SplitParticipantRequest
{
    public Guid UserId { get; set; }
    public decimal? Amount { get; set; }
    public double? Percentage { get; set; }
}
