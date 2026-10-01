using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Groups;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ICurrentUserService _currentUser;

    public GroupController(IGroupService groupService, ICurrentUserService currentUser)
    {
        _groupService = groupService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<GroupResponse>> CreateGroup(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _groupService.CreateGroupAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetMyGroups), result);
    }

    [HttpPost("{groupId:guid}/members")]
    public async Task<ActionResult<GroupMemberResponse>> AddMember(
        [FromRoute] Guid groupId,
        [FromBody] AddGroupMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _groupService.AddMemberAsync(groupId, request, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<GroupResponse>>> GetMyGroups(CancellationToken cancellationToken)
    {
        var result = await _groupService.GetMyGroupsAsync(_currentUser.UserId, cancellationToken);
        return Ok(result);
    }
}
