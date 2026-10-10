using FairShare.Application.DTOs.Files;
using FairShare.Application.DTOs.Profile;

namespace FairShare.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileResponse> GetMyProfileAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ProfileResponse> UpdateMyProfileAsync(
        Guid currentUserId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    // NEW: profile image

    Task<ProfileResponse> UploadProfileImageAsync(
        Guid currentUserId,
        ImageUpload upload,
        CancellationToken cancellationToken = default);

    Task DeleteProfileImageAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ImageFile> GetProfileImageAsync(Guid userId, CancellationToken cancellationToken = default);
}
