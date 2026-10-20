using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Categories;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

/// <summary>
/// Categories: every user reads the approved list and may propose a new one; the administrator
/// reviews proposals and maintains the list.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryController : ControllerBase
{
    private const string AdminRole = "Admin";

    private readonly ICategoryService _categoryService;
    private readonly ICurrentUserService _currentUser;

    public CategoryController(ICategoryService categoryService, ICurrentUserService currentUser)
    {
        _categoryService = categoryService;
        _currentUser = currentUser;
    }

    /// <summary>Approved categories.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _categoryService.GetAllAsync(cancellationToken));

    /// <summary>
    /// A user proposes a category (Status = Pending); an administrator adds an approved one
    /// at once (Status = Approved).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CategoryProposalResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>The signed-in user's proposals and the decisions on them.</summary>
    [HttpGet("proposals/mine")]
    public async Task<ActionResult<IReadOnlyList<CategoryProposalResponse>>> GetMyProposals(CancellationToken cancellationToken)
        => Ok(await _categoryService.GetMyProposalsAsync(_currentUser.UserId, cancellationToken));

    // ---------- administrator ----------

    /// <summary>All categories with their status and usage; ?status=Pending for the proposals only.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = AdminRole)]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryResponse>>> GetForAdmin(
        [FromQuery] CategoryStatus? status,
        CancellationToken cancellationToken)
        => Ok(await _categoryService.GetForAdminAsync(status, cancellationToken));

    [HttpPost("{categoryId:guid}/approve")]
    [Authorize(Roles = AdminRole)]
    public async Task<ActionResult<AdminCategoryResponse>> Approve(
        [FromRoute] Guid categoryId,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
        => Ok(await _categoryService.ApproveAsync(categoryId, request, cancellationToken));

    [HttpPost("{categoryId:guid}/reject")]
    [Authorize(Roles = AdminRole)]
    public async Task<ActionResult<AdminCategoryResponse>> Reject(
        [FromRoute] Guid categoryId,
        [FromBody] RejectCategoryRequest request,
        CancellationToken cancellationToken)
        => Ok(await _categoryService.RejectAsync(categoryId, request, cancellationToken));

    [HttpPut("{categoryId:guid}")]
    [Authorize(Roles = AdminRole)]
    public async Task<ActionResult<AdminCategoryResponse>> Update(
        [FromRoute] Guid categoryId,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
        => Ok(await _categoryService.UpdateAsync(categoryId, request, cancellationToken));

    [HttpDelete("{categoryId:guid}")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Delete([FromRoute] Guid categoryId, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(categoryId, cancellationToken);
        return NoContent();
    }
}
