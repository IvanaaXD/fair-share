namespace FairShare.Application.DTOs.Profile;

public class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>ISO 4217 ознака од 3 слова, нпр. BAM, EUR, RSD.</summary>
    public string DefaultCurrency { get; set; } = "BAM";

    /// <summary>Може садржати цртице и размаке; чува се само 16 или 18 цифара. Празно = брише рачун.</summary>
    public string? BankAccountNumber { get; set; }
}
