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
        return CreatedAtAction(nameof(GetGroup), new { groupId = result.Id }, result);
    }

    /// <summary>All groups the current user is a member of.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<GroupResponse>>> GetMyGroups(CancellationToken cancellationToken)
    {
        var result = await _groupService.GetMyGroupsAsync(_currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Group details with members. Members only.</summary>
    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<GroupResponse>> GetGroup(
        [FromRoute] Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _groupService.GetGroupAsync(groupId, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Changes the group name and description. Owner only.</summary>
    [HttpPut("{groupId:guid}")]
    public async Task<ActionResult<GroupResponse>> UpdateGroup(
        [FromRoute] Guid groupId,
        [FromBody] UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _groupService.UpdateGroupAsync(groupId, request, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Deletes the group. Owner only, and only when nobody in the group owes anything.</summary>
    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> DeleteGroup([FromRoute] Guid groupId, CancellationToken cancellationToken)
    {
        await _groupService.DeleteGroupAsync(groupId, _currentUser.UserId, cancellationToken);
        return NoContent();
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

    /// <summary>Removes a member. Owner only, and only when that member's balance is settled.</summary>
    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] Guid groupId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        await _groupService.RemoveMemberAsync(groupId, userId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    /// <summary>The current user leaves the group, allowed only when their balance is settled.</summary>
    [HttpPost("{groupId:guid}/leave")]
    public async Task<IActionResult> LeaveGroup([FromRoute] Guid groupId, CancellationToken cancellationToken)
    {
        await _groupService.LeaveGroupAsync(groupId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
