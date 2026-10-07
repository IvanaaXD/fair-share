using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Profile;

public class ProfileResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = string.Empty;
    public string? BankAccountNumber { get; set; }
    public string? ProfileImageUrl { get; set; }
    public UserRole Role { get; set; }
}
