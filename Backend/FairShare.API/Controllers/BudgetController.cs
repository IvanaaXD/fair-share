using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Budgets;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ICurrentUserService _currentUser;

    public BudgetController(IBudgetService budgetService, ICurrentUserService currentUser)
    {
        _budgetService = budgetService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<BudgetResponse>> Create(
        [FromBody] CreateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _budgetService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetMine), new { month = result.Month }, result);
    }

    [HttpPut("{budgetId:guid}")]
    public async Task<ActionResult<BudgetResponse>> Update(
        [FromRoute] Guid budgetId,
        [FromBody] UpdateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _budgetService.UpdateAsync(budgetId, request, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{budgetId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid budgetId, CancellationToken cancellationToken)
    {
        await _budgetService.DeleteAsync(budgetId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    /// <summary>Буџети тренутног корисника за дати мјесец (подразумијевано: текући мјесец).</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<BudgetResponse>>> GetMine(
        [FromQuery] string? month,
        CancellationToken cancellationToken)
    {
        var targetMonth = string.IsNullOrWhiteSpace(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month;
        var result = await _budgetService.GetMyBudgetsAsync(_currentUser.UserId, targetMonth, cancellationToken);
        return Ok(result);
    }
}
