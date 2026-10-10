using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Files;
using FairShare.Application.DTOs.Files;
using FairShare.Application.DTOs.Profile;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;

namespace FairShare.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IImageStorageService _images; // NEW

    public ProfileService(IUnitOfWork unitOfWork, IMapper mapper, IImageStorageService images)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _images = images;
    }

    public async Task<ProfileResponse> GetMyProfileAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(currentUserId, cancellationToken);
        return _mapper.Map<ProfileResponse>(user);
    }

    public async Task<ProfileResponse> UpdateMyProfileAsync(
        Guid currentUserId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        // Formats are checked by UpdateProfileRequestValidator; here values are only normalized.
        var user = await GetUserAsync(currentUserId, cancellationToken);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DefaultCurrency = request.DefaultCurrency.Trim().ToUpperInvariant();

        // "161-0000012345678-90" is stored as "161000001234567890"; an empty value removes the account.
        user.BankAccountNumber = string.IsNullOrWhiteSpace(request.BankAccountNumber)
            ? null
            : IpsQrCodec.NormalizeAccount(request.BankAccountNumber);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProfileResponse>(user);
    }

    // ---------- NEW: profile image ----------

    public async Task<ProfileResponse> UploadProfileImageAsync(
        Guid currentUserId,
        ImageUpload upload,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(currentUserId, cancellationToken);

        // File first, database second: a failed upload leaves the old image in place.
        await _images.SaveAsync(StorageKeys.ProfileImage(user.Id), upload, cancellationToken);

        user.ProfileImageUrl = ImageUrls.ProfileImage(user.Id);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProfileResponse>(user);
    }

    public async Task DeleteProfileImageAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(currentUserId, cancellationToken);

        if (user.ProfileImageUrl is not null)
        {
            user.ProfileImageUrl = null;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _images.DeleteAsync(StorageKeys.ProfileImage(user.Id), cancellationToken);
    }

    /// <summary>
    /// Any signed-in user may see another user's profile image: it is shown next to names in
    /// groups, member lists and comments.
    /// </summary>
    public async Task<ImageFile> GetProfileImageAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _images.ReadAsync(StorageKeys.ProfileImage(userId), cancellationToken)
           ?? throw new NotFoundException("Корисник нема профилну слику.");

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        => await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
           ?? throw new NotFoundException("Корисник није пронађен.");
}
