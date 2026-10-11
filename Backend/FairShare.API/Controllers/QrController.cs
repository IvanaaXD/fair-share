using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Qr;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

[ApiController]
[Authorize]
public class QrController : ControllerBase
{
    private readonly IQrPaymentService _qrPaymentService;
    private readonly ICurrentUserService _currentUser;

    public QrController(IQrPaymentService qrPaymentService, ICurrentUserService currentUser)
    {
        _qrPaymentService = qrPaymentService;
        _currentUser = currentUser;
    }

    /// <summary>PNG image of a QR code for settlement payment.</summary>
    [HttpGet("api/groups/{groupId:guid}/settlements/{settlementId:guid}/qr")]
    [Produces("image/png")]
    public async Task<IActionResult> GetImage(
        [FromRoute] Guid groupId,
        [FromRoute] Guid settlementId,
        CancellationToken cancellationToken)
    {
        var png = await _qrPaymentService.GetImageAsync(groupId, settlementId, _currentUser.UserId, cancellationToken);
        return File(png, "image/png");
    }

    /// <summary>QR code text and fields (if the frontend renders the QR itself or displays payment details).</summary>
    [HttpGet("api/groups/{groupId:guid}/settlements/{settlementId:guid}/qr/payload")]
    public async Task<ActionResult<QrPayloadResponse>> GetPayload(
        [FromRoute] Guid groupId,
        [FromRoute] Guid settlementId,
        CancellationToken cancellationToken)
    {
        var result = await _qrPaymentService.GetPayloadAsync(groupId, settlementId, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Parses the text read from a QR code. An invalid QR returns 200 with IsValid=false and
    /// a list of errors, so the frontend can show the user what went wrong.
    /// </summary>
    [HttpPost("api/qr/parse")]
    public async Task<ActionResult<ParsedQrResponse>> Parse(
        [FromBody] ParseQrRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _qrPaymentService.ParseAsync(request, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }
}
