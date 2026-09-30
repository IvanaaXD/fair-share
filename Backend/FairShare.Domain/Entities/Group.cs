namespace FairShare.Domain.Entities;

public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Currency { get; set; } = "BAM";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<GroupExpense> Expenses { get; set; } = new List<GroupExpense>();
    public ICollection<SettlementTransaction> Settlements { get; set; } = new List<SettlementTransaction>();

    public void AddMember(GroupMember member) => Members.Add(member);

    public void RemoveMember(GroupMember member) => Members.Remove(member);
}
