using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Settlements;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

[ApiController]
[Route("api/groups/{groupId:guid}/settlements")]
[Authorize]
public class SettlementController : ControllerBase
{
    private readonly ISettlementService _settlementService;
    private readonly ICurrentUserService _currentUser;

    public SettlementController(ISettlementService settlementService, ICurrentUserService currentUser)
    {
        _settlementService = settlementService;
        _currentUser = currentUser;
    }

    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<BalanceResponse>>> GetBalances(
        [FromRoute] Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.GetGroupBalancesAsync(groupId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<IReadOnlyList<SettlementTransactionResponse>>> Generate(
        [FromRoute] Guid groupId,
        [FromQuery] bool useExact = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _settlementService.GenerateSettlementSuggestionsAsync(
            groupId, useExact, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SettlementTransactionResponse>>> GetAll(
        [FromRoute] Guid groupId,
        [FromQuery] SettlementStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.GetGroupSettlementsAsync(groupId, status, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{settlementId:guid}/settle")]
    public async Task<ActionResult<SettlementTransactionResponse>> MarkSettled(
        [FromRoute] Guid groupId,
        [FromRoute] Guid settlementId,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.MarkAsSettledAsync(settlementId, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }
}
