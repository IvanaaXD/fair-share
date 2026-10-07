using FairShare.Application.DTOs.Profile;

namespace FairShare.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileResponse> GetMyProfileAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    Task<ProfileResponse> UpdateMyProfileAsync(
        Guid currentUserId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
}
