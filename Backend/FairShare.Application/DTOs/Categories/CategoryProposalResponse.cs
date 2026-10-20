using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Categories;

/// <summary>A category the signed-in user proposed, with the administrator's decision.</summary>
public class CategoryProposalResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public CategoryStatus Status { get; set; }
    public DateTime? ProposedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}
