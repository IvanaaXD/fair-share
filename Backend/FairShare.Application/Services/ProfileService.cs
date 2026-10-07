using System.Text.RegularExpressions;
using FairShare.Application.DTOs.Profile;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class ProfileService : IProfileService
{
    private static readonly Regex CurrencyRegex = new(@"^[A-Z]{3}$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;

    public ProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProfileResponse> GetMyProfileAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

        return MapToResponse(user);
    }

    public async Task<ProfileResponse> UpdateMyProfileAsync(
        Guid currentUserId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new ConflictException("Име и презиме су обавезни.");

        var currency = (request.DefaultCurrency ?? string.Empty).Trim().ToUpperInvariant();
        if (!CurrencyRegex.IsMatch(currency))
            throw new ConflictException("Валута мора бити ознака од 3 слова (нпр. BAM, EUR, RSD).");

        string? account = null;
        if (!string.IsNullOrWhiteSpace(request.BankAccountNumber))
        {
            account = IpsQrCodec.NormalizeAccount(request.BankAccountNumber);
            if (!IpsQrCodec.IsValidAccount(account))
                throw new ConflictException("Број рачуна мора имати 16 или 18 цифара.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DefaultCurrency = currency;
        user.BankAccountNumber = account;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(user);
    }

    private static ProfileResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        DefaultCurrency = user.DefaultCurrency,
        BankAccountNumber = user.BankAccountNumber,
        ProfileImageUrl = user.ProfileImageUrl,
        Role = user.Role
    };
}
