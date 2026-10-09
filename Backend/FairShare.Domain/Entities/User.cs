using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Always stored normalized (see <see cref="NormalizeEmail"/>).</summary>
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string DefaultCurrency { get; set; } = "BAM";

    // Bank account number (digits only) for IPS-style QR payments; null until the user enters it.
    public string? BankAccountNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsBlocked { get; set; }

    // Forces a password change before the rest of the system can be used
    // (true for the seeded administrator, false after the change).
    public bool MustChangePassword { get; set; }

    // NEW: true from registration until the user opens the activation link from the e-mail.
    // While it is true the user cannot sign in. The default is false, so accounts that existed
    // before this feature (e.g. the seeded administrator) stay usable without a data migration.
    public bool MustConfirmEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<PaymentCard> PaymentCards { get; set; } = new List<PaymentCard>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Settlements in which the user appears as the debtor or as the creditor
    public ICollection<SettlementTransaction> SettlementsAsDebtor { get; set; } = new List<SettlementTransaction>();
    public ICollection<SettlementTransaction> SettlementsAsCreditor { get; set; } = new List<SettlementTransaction>();

    public void ChangePassword(string newPasswordHash) => PasswordHash = newPasswordHash;

    public void Deactivate() => IsBlocked = true;

    public void Activate() => IsBlocked = false;

    // NEW: single place that defines how e-mail addresses are compared. PostgreSQL compares text
    // case-sensitively, so without this "Ana@x.com" and "ana@x.com" would be two accounts.
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
