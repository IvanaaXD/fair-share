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
        return CreatedAtAction(nameof(GetByGroup), new { groupId }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupExpenseResponse>>> GetByGroup(
        [FromRoute] Guid groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _groupExpenseService.GetByGroupAsync(groupId, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
