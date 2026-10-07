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

    /// <summary>Current net balance of every member of the group. Members only.</summary>
    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<BalanceResponse>>> GetBalances(
        [FromRoute] Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.GetGroupBalancesAsync(groupId, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Generates a new settlement suggestion. useExact=true uses the exact algorithm (small groups only).</summary>
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

    /// <summary>Settlements of the group, optionally filtered by status. Members only.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SettlementTransactionResponse>>> GetAll(
        [FromRoute] Guid groupId,
        [FromQuery] SettlementStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.GetGroupSettlementsAsync(
            groupId, status, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Marks a settlement as paid. Only its debtor or creditor may do it.</summary>
    [HttpPost("{settlementId:guid}/settle")]
    public async Task<ActionResult<SettlementTransactionResponse>> MarkSettled(
        [FromRoute] Guid groupId,
        [FromRoute] Guid settlementId,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.MarkAsSettledAsync(
            groupId, settlementId, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }
}
