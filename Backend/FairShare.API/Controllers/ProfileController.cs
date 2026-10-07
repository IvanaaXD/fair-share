using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Profile;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ICurrentUserService _currentUser;

    public ProfileController(IProfileService profileService, ICurrentUserService currentUser)
    {
        _profileService = profileService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> GetMine(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetMyProfileAsync(_currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateMyProfileAsync(_currentUser.UserId, request, cancellationToken);
        return Ok(result);
    }
}
