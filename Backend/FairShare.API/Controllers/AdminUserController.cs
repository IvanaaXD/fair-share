using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Users;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUserController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUser;

    public AdminUserController(IUserManagementService userManagementService, ICurrentUserService currentUser)
    {
        _userManagementService = userManagementService;
        _currentUser = currentUser;
    }

    /// <summary>Претрага корисника по имену/email-у, уз опционо филтрирање по статусу блокираности.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> Search(
        [FromQuery] string? search,
        [FromQuery] bool? isBlocked,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _userManagementService.SearchAsync(search, isBlocked, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{userId:guid}/block")]
    public async Task<IActionResult> Block([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        await _userManagementService.BlockAsync(userId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/unblock")]
    public async Task<IActionResult> Unblock([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        await _userManagementService.UnblockAsync(userId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
