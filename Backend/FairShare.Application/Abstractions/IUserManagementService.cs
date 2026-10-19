using FairShare.Application.DTOs.Users;

namespace FairShare.Application.Interfaces;

public interface IUserManagementService
{
    /// <summary>Search and filter users for administrative review </summary>
    Task<IReadOnlyList<UserResponse>> SearchAsync(
        string? searchTerm,
        bool? isBlocked,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task BlockAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default);

    Task UnblockAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default);

    /// <summary>Change own password; removes the MustChangePassword flag upon success.</summary>
    Task ChangeMyPasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
