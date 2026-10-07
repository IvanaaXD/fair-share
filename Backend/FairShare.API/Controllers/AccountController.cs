using FairShare.Api.Filters;
using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Users;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUser;

    public AccountController(IUserManagementService userManagementService, ICurrentUserService currentUser)
    {
        _userManagementService = userManagementService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Промјена сопствене лозинке. Означено са [AllowWhilePasswordChangeRequired] -
    /// доступно и кориснику коме RequirePasswordChangeFilter иначе блокира приступ.
    /// </summary>
    [HttpPost("change-password")]
    [AllowWhilePasswordChangeRequired]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userManagementService.ChangeMyPasswordAsync(_currentUser.UserId, request, cancellationToken);
        return NoContent();
    }
}
