using FairShare.Application.Abstractions;
using FairShare.Application.Common.Files;
using FairShare.Application.DTOs.Files;
using FairShare.Application.Interfaces;
using FairShare.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

/// <summary>
/// Receipt photo of a personal expense. The expense is created first (JSON), then the photo is
/// uploaded here; only the owner of the expense can upload, see or remove it.
/// </summary>
[ApiController]
[Route("api/expenses/{expenseId:guid}/receipt")]
[Authorize]
public class ExpenseReceiptController : ControllerBase
{
    private readonly IReceiptService _receiptService;
    private readonly ICurrentUserService _currentUser;

    public ExpenseReceiptController(IReceiptService receiptService, ICurrentUserService currentUser)
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
        [FromRoute] Guid expenseId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var result = await _receiptService.UploadExpenseReceiptAsync(
            expenseId, _currentUser.UserId, new ImageUpload(content, file.Length), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromRoute] Guid expenseId, CancellationToken cancellationToken)
    {
        var image = await _receiptService.GetExpenseReceiptAsync(expenseId, _currentUser.UserId, cancellationToken);
        return this.ImageResult(image);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] Guid expenseId, CancellationToken cancellationToken)
    {
        await _receiptService.DeleteExpenseReceiptAsync(expenseId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
