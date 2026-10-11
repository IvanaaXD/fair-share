namespace FairShare.Application.DTOs.Profile;

public class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>3-letter ISO 4217 code, e.g., BAM, EUR, RSD.</summary>
    public string DefaultCurrency { get; set; } = "BAM";

    /// <summary>Can contain dashes and spaces; only 16 or 18 digits are stored. Empty = deletes the account.</summary>
    public string? BankAccountNumber { get; set; }
}
