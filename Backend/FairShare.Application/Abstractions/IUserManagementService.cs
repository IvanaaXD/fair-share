using FairShare.Application.DTOs.Users;

namespace FairShare.Application.Interfaces;

public interface IUserManagementService
{
    /// <summary>Претрага и филтрирање корисника за администраторски преглед (функционалност 5.2).</summary>
    Task<IReadOnlyList<UserResponse>> SearchAsync(
        string? searchTerm,
        bool? isBlocked,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task BlockAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default);

    Task UnblockAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default);

    /// <summary>Промјена сопствене лозинке; скида MustChangePassword заставицу након успјеха.</summary>
    Task ChangeMyPasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
