using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Profile;
using FairShare.Application.Interfaces;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProfileService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProfileResponse> GetMyProfileAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

        return _mapper.Map<ProfileResponse>(user);
    }

    public async Task<ProfileResponse> UpdateMyProfileAsync(
        Guid currentUserId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        // Formats are checked by UpdateProfileRequestValidator; here values are only normalized.
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

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
}
