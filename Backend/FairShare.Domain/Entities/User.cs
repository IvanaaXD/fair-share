using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string DefaultCurrency { get; set; } = "BAM";

    // НОВО: број рачуна (само цифре) за QR уплате по узору на IPS; null док га корисник не унесе.
    public string? BankAccountNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsBlocked { get; set; }

    // Приморава промјену лозинке прије приступа остатку система.
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационе особине
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<PaymentCard> PaymentCards { get; set; } = new List<PaymentCard>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Поравнања у којима се корисник појављује као дужник, односно повјерилац
    public ICollection<SettlementTransaction> SettlementsAsDebtor { get; set; } = new List<SettlementTransaction>();
    public ICollection<SettlementTransaction> SettlementsAsCreditor { get; set; } = new List<SettlementTransaction>();

    public void ChangePassword(string newPasswordHash) => PasswordHash = newPasswordHash;

    public void Deactivate() => IsBlocked = true;

    public void Activate() => IsBlocked = false;
}
