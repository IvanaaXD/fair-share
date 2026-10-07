namespace FairShare.Application.DTOs.Admin;

public class AuditLogResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>Облик "Контролер.Акција", нпр. "AdminUser.Block" или "Auth.Login".</summary>
    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public DateTime Timestamp { get; set; }
}
