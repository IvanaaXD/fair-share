using FairShare.Application.Abstractions;
using FairShare.Application.Common.Files;
using FairShare.Application.DTOs.Files;
using FairShare.Application.Interfaces;
using FairShare.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

/// <summary>
/// Receipt photo of a group expense. Every member of the group can see it; the member who paid
/// the expense and the group owner can upload or remove it.
/// </summary>
[ApiController]
[Route("api/groups/{groupId:guid}/expenses/{groupExpenseId:guid}/receipt")]
[Authorize]
public class GroupExpenseReceiptController : ControllerBase
{
    private readonly IReceiptService _receiptService;
    private readonly ICurrentUserService _currentUser;

    public GroupExpenseReceiptController(IReceiptService receiptService, ICurrentUserService currentUser)
    {
        _receiptService = receiptService;
        _currentUser = currentUser;
    }

    /// <summary>Sets or replaces the receipt (multipart/form-data, field "file"; JPEG, PNG or WebP, at most 5 MB).</summary>
    [HttpPut]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageFileRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageFileRules.MaxRequestBytes)]
    public async Task<ActionResult<ImageUrlResponse>> Upload(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var result = await _receiptService.UploadGroupExpenseReceiptAsync(
            groupId, groupExpenseId, _currentUser.UserId, new ImageUpload(content, file.Length), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        CancellationToken cancellationToken)
    {
        var image = await _receiptService.GetGroupExpenseReceiptAsync(
            groupId, groupExpenseId, _currentUser.UserId, cancellationToken);
        return this.ImageResult(image);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        CancellationToken cancellationToken)
    {
        await _receiptService.DeleteGroupExpenseReceiptAsync(
            groupId, groupExpenseId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
