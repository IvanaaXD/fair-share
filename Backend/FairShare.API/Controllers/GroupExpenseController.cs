using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.GroupExpenses;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

[ApiController]
[Route("api/groups/{groupId:guid}/expenses")]
[Authorize]
public class GroupExpenseController : ControllerBase
{
    private readonly IGroupExpenseService _groupExpenseService;
    private readonly ICurrentUserService _currentUser;

    public GroupExpenseController(IGroupExpenseService groupExpenseService, ICurrentUserService currentUser)
    {
        _groupExpenseService = groupExpenseService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<GroupExpenseResponse>> Create(
        [FromRoute] Guid groupId,
        [FromBody] CreateGroupExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _groupExpenseService.CreateAsync(groupId, request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { groupId, groupExpenseId = result.Id }, result);
    }

    /// <summary>Paged list of the group's expenses, newest first. Members only.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupExpenseResponse>>> GetByGroup(
        [FromRoute] Guid groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _groupExpenseService.GetByGroupAsync(
            groupId, page, pageSize, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{groupExpenseId:guid}")]
    public async Task<ActionResult<GroupExpenseResponse>> GetById(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        CancellationToken cancellationToken)
    {
        var result = await _groupExpenseService.GetByIdAsync(
            groupId, groupExpenseId, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Replaces the expense and recalculates its splits. Allowed for the member who paid
    /// the expense and for the group owner.
    /// </summary>
    [HttpPut("{groupExpenseId:guid}")]
    public async Task<ActionResult<GroupExpenseResponse>> Update(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        [FromBody] UpdateGroupExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _groupExpenseService.UpdateAsync(
            groupId, groupExpenseId, request, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{groupExpenseId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        CancellationToken cancellationToken)
    {
        await _groupExpenseService.DeleteAsync(groupId, groupExpenseId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
