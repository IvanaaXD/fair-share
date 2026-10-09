using AutoMapper;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Users;
using FairShare.Application.Interfaces;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public UserManagementService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<UserResponse>> SearchAsync(
        string? searchTerm,
        bool? isBlocked,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.SearchAsync(searchTerm, isBlocked, page, pageSize, cancellationToken);
        return _mapper.Map<List<UserResponse>>(users);
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
        // Password strength is checked by ChangePasswordRequestValidator; here we check
        // what needs the stored hash.
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new ForbiddenException("Тренутна лозинка није тачна.");

        user.ChangePassword(_passwordHasher.HashPassword(request.NewPassword));
        user.MustChangePassword = false;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
