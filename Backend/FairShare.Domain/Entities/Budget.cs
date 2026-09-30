namespace FairShare.Domain.Entities;

public class Budget : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal MonthlyLimit { get; set; }

    // Формат "ГГГГ-ММ", нпр. "2026-09"
    public string Month { get; set; } = string.Empty;

    public bool CheckThreshold(decimal currentSpending, double thresholdPercentage = 0.8)
        => currentSpending >= MonthlyLimit * (decimal)thresholdPercentage;
}
