using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Comments;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

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

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid groupId,
        [FromRoute] Guid groupExpenseId,
        [FromRoute] Guid commentId,
        CancellationToken cancellationToken)
    {
        await _commentService.DeleteAsync(commentId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
