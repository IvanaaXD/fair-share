using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Expenses;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpenseController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ICurrentUserService _currentUser;

    public ExpenseController(IExpenseService expenseService, ICurrentUserService currentUser)
    {
        _expenseService = expenseService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetMine), result);
    }

    [HttpPut("{expenseId:guid}")]
    public async Task<ActionResult<ExpenseResponse>> Update(
        [FromRoute] Guid expenseId,
        [FromBody] UpdateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.UpdateAsync(expenseId, request, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{expenseId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid expenseId, CancellationToken cancellationToken)
    {
        await _expenseService.DeleteAsync(expenseId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    /// <summary>Претрага личних трошкова; сви параметри су опциони.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<ExpenseResponse>>> GetMine(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetMyExpensesAsync(_currentUser.UserId, from, to, categoryId, cancellationToken);
        return Ok(result);
    }
}
