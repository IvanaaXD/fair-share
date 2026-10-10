using FairShare.Application.Abstractions;
using FairShare.Application.Common.Files;
using FairShare.Application.DTOs.Files;
using FairShare.Application.DTOs.Profile;
using FairShare.Application.Interfaces;
using FairShare.WebAPI.Extensions;
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

    // ---------- NEW: profile image ----------

    /// <summary>
    /// Sets or replaces the profile image (multipart/form-data, field "file"; JPEG, PNG or WebP,
    /// at most 5 MB). Returns the profile with the new ProfileImageUrl.
    /// </summary>
    [HttpPut("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageFileRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageFileRules.MaxRequestBytes)]
    public async Task<ActionResult<ProfileResponse>> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var result = await _profileService.UploadProfileImageAsync(
            _currentUser.UserId, new ImageUpload(content, file.Length), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("image")]
    public async Task<IActionResult> DeleteImage(CancellationToken cancellationToken)
    {
        await _profileService.DeleteProfileImageAsync(_currentUser.UserId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Any user's profile image (this is the address stored in ProfileImageUrl). Requires sign-in
    /// like every other endpoint, so the frontend downloads it with the token (see Napomene.txt).
    /// </summary>
    [HttpGet("~/api/users/{userId:guid}/profile-image")]
    public async Task<IActionResult> GetImage([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var image = await _profileService.GetProfileImageAsync(userId, cancellationToken);
        return this.ImageResult(image);
    }
}
