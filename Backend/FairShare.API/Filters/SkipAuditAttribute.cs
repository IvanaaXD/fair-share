namespace FairShare.Api.Filters;

/// <summary>
/// Искључује акцију из ревизионог дневника - за POST/PUT/DELETE endpoint-е који не мијењају
/// важне податке (нпр. означавање обавјештења као прочитаног, парсирање QR кода).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipAuditAttribute : Attribute
{
}
