namespace FairShare.Application.DTOs.GroupExpenses;

/// <summary>
/// Editing a group expense takes exactly the same data as creating one (the whole expense is
/// sent again and its splits are recalculated), so the fields are inherited instead of repeated.
/// A separate type keeps the API contract explicit and lets the update get its own validator.
/// </summary>
public class UpdateGroupExpenseRequest : CreateGroupExpenseRequest
{
}
