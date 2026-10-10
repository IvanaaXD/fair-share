using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Common;
using FairShare.Application.DTOs.Expenses;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.Api.Controllers;

/// <summary>
/// Personal expenses of the signed-in user. Every endpoint works only with the user's own
/// expenses, so the list is simply GET /api/expenses (no "mine" needed).
/// </summary>
[ApiController]
[Route("api/expenses")] // CHANGED from api/[controller] (= api/Expense), matching api/expenses/{id}/receipt
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

    /// <summary>
    /// Search with paging, e.g. ?search=stan&amp;from=2026-10-01&amp;to=2026-10-31&amp;sortBy=Amount&amp;page=1&amp;pageSize=20.
    /// "from" and "to" are dates, both days included.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ExpenseResponse>>> GetMine(
        [FromQuery] ExpenseQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetMyExpensesAsync(_currentUser.UserId, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{expenseId:guid}")]
    public async Task<ActionResult<ExpenseResponse>> GetById([FromRoute] Guid expenseId, CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetByIdAsync(expenseId, _currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates an expense. With isRecurring = true and an interval it becomes a template:
    /// a copy is added automatically every day/week/month/year (see nextOccurrenceDate).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        // CHANGED: Location header now points to the new expense, not to the whole list.
        return CreatedAtAction(nameof(GetById), new { expenseId = result.Id }, result);
    }

    /// <summary>Edits an expense. Setting isRecurring = false on a template stops its automatic copies.</summary>
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
}
