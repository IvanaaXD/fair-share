using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Payments;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

[ApiController]
[Route("api/groups/{groupId:guid}/settlements/{settlementId:guid}/pay")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUser;

    public PaymentController(IPaymentService paymentService, ICurrentUserService currentUser)
    {
        _paymentService = paymentService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// The debtor pays the settlement with a saved card. Returns 200 also when the card is
    /// declined - then Status = "Failed" and FailureReason says why, so the user can try again.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> PayWithCard(
        [FromRoute] Guid groupId,
        [FromRoute] Guid settlementId,
        [FromBody] PayWithCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.PayWithCardAsync(
            groupId, settlementId, request, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }
}
