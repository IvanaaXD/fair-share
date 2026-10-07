using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Users;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public UserManagementService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserResponse>> SearchAsync(
        string? searchTerm,
        bool? isBlocked,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.SearchAsync(searchTerm, isBlocked, page, pageSize, cancellationToken);
        return users.Select(MapToResponse).ToList();
    }

    public async Task BlockAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default)
    {
        if (userId == currentAdminId)
            throw new ConflictException("Не можете блокирати сопствени налог.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

        user.Deactivate();
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnblockAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

        user.Activate();
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeMyPasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new ForbiddenException("Тренутна лозинка није тачна.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ConflictException("Нова лозинка мора имати најмање 8 карактера.");

        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            throw new ConflictException("Нова лозинка мора бити различита од тренутне.");

        var newHash = _passwordHasher.HashPassword(request.NewPassword);
        user.ChangePassword(newHash);
        user.MustChangePassword = false;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        IsBlocked = user.IsBlocked,
        MustChangePassword = user.MustChangePassword,
        CreatedAt = user.CreatedAt
    };
}
