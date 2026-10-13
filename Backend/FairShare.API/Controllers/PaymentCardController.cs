using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Payments;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

/// <summary>The signed-in user's saved payment cards (only tokens and display data are stored).</summary>
[ApiController]
[Route("api/payment-cards")]
[Authorize]
public class PaymentCardController : ControllerBase
{
    private readonly IPaymentCardService _cardService;
    private readonly ICurrentUserService _currentUser;

    public PaymentCardController(IPaymentCardService cardService, ICurrentUserService currentUser)
    {
        _cardService = cardService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentCardResponse>>> GetMine(CancellationToken cancellationToken)
        => Ok(await _cardService.GetMyCardsAsync(_currentUser.UserId, cancellationToken));

    /// <summary>
    /// Adds a card. Test numbers of the payment simulator: 4242 4242 4242 4242 (success),
    /// 4000 0000 0000 0002 (declined), 4000 0000 0000 9995 (insufficient funds).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PaymentCardResponse>> Add(
        [FromBody] AddPaymentCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cardService.AddCardAsync(_currentUser.UserId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{paymentCardId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid paymentCardId, CancellationToken cancellationToken)
    {
        await _cardService.DeleteCardAsync(_currentUser.UserId, paymentCardId, cancellationToken);
        return NoContent();
    }
}
