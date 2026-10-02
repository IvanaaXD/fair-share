using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Categories;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ICurrentUserService _currentUser;

    public CategoryController(ICategoryService categoryService, ICurrentUserService currentUser)
    {
        _categoryService = categoryService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        [FromRoute] Guid categoryId,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(categoryId, request, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid categoryId, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(categoryId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
