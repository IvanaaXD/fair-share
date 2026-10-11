namespace FairShare.Api.Filters;

/// <summary>
/// Marks an endpoint that remains accessible even when the user has MustChangePassword = true
/// (e.g., the password change endpoint itself).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AllowWhilePasswordChangeRequiredAttribute : Attribute
{
}
