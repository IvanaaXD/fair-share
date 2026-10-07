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

    /// <summary>PNG слика QR кода за уплату по поравнању.</summary>
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

    /// <summary>Текст QR кода и поља (ако frontend сам исцртава QR или приказује детаље уплате).</summary>
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
    /// Парсира текст прочитан из QR кода. Невалидан QR враћа 200 са IsValid=false и
    /// листом грешака, да frontend може кориснику показати шта није у реду.
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
