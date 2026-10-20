using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

/// <summary>
/// One list of categories shared by all users. Only approved categories can be chosen for
/// expenses and budgets; a user's proposal becomes one after an administrator approves it.
/// </summary>
public class Category : BaseEntity
{
    public const int MaxNameLength = 50;
    public const int MaxRejectionReasonLength = 300;

    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }

    /// <summary>Came with the application (seeded), not added later.</summary>
    public bool IsSystemDefined { get; set; }

    public CategoryStatus Status { get; private set; } = CategoryStatus.Approved;

    /// <summary>The user who proposed it; null for seeded categories and those added by an administrator.</summary>
    public Guid? ProposedByUserId { get; private set; }
    public User? ProposedByUser { get; private set; }
    public DateTime? ProposedAt { get; private set; }

    /// <summary>When an administrator approved or rejected the proposal.</summary>
    public DateTime? ReviewedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<GroupExpense> GroupExpenses { get; set; } = new List<GroupExpense>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    public bool IsApproved => Status == CategoryStatus.Approved;
    public bool IsPending => Status == CategoryStatus.Pending;

    /// <summary>A category added by an administrator - usable at once.</summary>
    public static Category CreateApproved(string name, string? icon)
        => new() { Name = name, Icon = icon, Status = CategoryStatus.Approved };

    /// <summary>A user's proposal - waits for an administrator.</summary>
    public static Category Propose(string name, string? icon, Guid userId, DateTime utcNow)
        => new()
        {
            Name = name,
            Icon = icon,
            Status = CategoryStatus.Pending,
            ProposedByUserId = userId,
            ProposedAt = utcNow
        };

    /// <summary>The administrator may correct the name and icon while approving.</summary>
    public void Approve(string name, string? icon, DateTime utcNow)
    {
        EnsurePending();
        Name = name;
        Icon = icon;
        Status = CategoryStatus.Approved;
        ReviewedAt = utcNow;
        RejectionReason = null;
    }

    public void Reject(string reason, DateTime utcNow)
    {
        EnsurePending();
        Status = CategoryStatus.Rejected;
        ReviewedAt = utcNow;
        RejectionReason = reason;
    }

    private void EnsurePending()
    {
        if (!IsPending)
            throw new InvalidOperationException("Only a pending category can be approved or rejected.");
    }
}
