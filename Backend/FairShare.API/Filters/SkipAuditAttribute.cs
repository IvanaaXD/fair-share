namespace FairShare.Api.Filters;

/// <summary>
/// Excludes the action from the audit log - for POST/PUT/DELETE endpoints that do not change
/// important data (e.g., marking a notification as read, parsing a QR code).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipAuditAttribute : Attribute
{
}
