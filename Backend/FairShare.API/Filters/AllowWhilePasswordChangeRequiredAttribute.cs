namespace FairShare.Api.Filters;

/// <summary>
/// Означава endpoint који остаје доступан и када корисник има MustChangePassword = true
/// (нпр. сам endpoint за промјену лозинке).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AllowWhilePasswordChangeRequiredAttribute : Attribute
{
}
