using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Categories;

/// <summary>A category as the administrator sees it: status, who proposed it and how much it is used.</summary>
public class AdminCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsSystemDefined { get; set; }
    public CategoryStatus Status { get; set; }

    public string? ProposedByName { get; set; }
    public string? ProposedByEmail { get; set; }
    public DateTime? ProposedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Expenses, group expenses and budgets in this category. Only 0 can be deleted.</summary>
    public int UsageCount { get; set; }
}
