namespace FairShare.Infrastructure.Email;

/// <summary>Попуњава се из секције "EmailSettings" у appsettings.json.</summary>
public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "FairShare";
}
