using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Comments;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

/// <summary>Short comments under a group expense ("this is for the coffee at the gas station").</summary>
[ApiController]
[Route("api/groups/{groupId:guid}/expenses/{groupExpenseId:guid}/comments")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ICurrentUserService _currentUser;

    public CommentController(ICommentService commentService, ICurrentUserService currentUser)
    {
        _commentService = commentService;
        _currentUser = currentUser;
    }

    /// <summary>Adds a comment. Members only; the payer and participants are notified in the app.</summary>
    [HttpPost]
    public async Task<ActionResult<CommentResponse>> Create(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _commentService.CreateAsync(
            groupId, groupExpenseId, request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { groupId, groupExpenseId }, result);
    }

    /// <summary>Comments of the expense, oldest first. Members only.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommentResponse>>> GetAll(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        CancellationToken cancellationToken)
    {
        var result = await _commentService.GetByGroupExpenseAsync(
            groupId, groupExpenseId, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Deletes a comment. Its author or the group owner only.</summary>
    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        [FromRoute] Guid commentId,
        CancellationToken cancellationToken)
    {
        await _commentService.DeleteAsync(groupId, groupExpenseId, commentId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
