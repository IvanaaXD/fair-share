namespace FairShare.Application.DTOs.Settlements;

/// <summary>Body of POST .../settlements/{id}/remind.</summary>
public class SendReminderRequest
{
    /// <summary>A short personal message; without it the debtor gets the standard text.</summary>
    public string? Message { get; set; }
}
