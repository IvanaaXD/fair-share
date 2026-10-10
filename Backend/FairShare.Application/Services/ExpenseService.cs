using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Common;
using FairShare.Application.DTOs.Expenses;
using FairShare.Application.Interfaces;
using FairShare.Application.Mappings;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Models;

namespace FairShare.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBudgetAlertService _budgetAlerts; // CHANGED: budget check moved to its own service
    private readonly IMapper _mapper;

    public ExpenseService(IUnitOfWork unitOfWork, IBudgetAlertService budgetAlerts, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _budgetAlerts = budgetAlerts;
        _mapper = mapper;
    }

    public async Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Field formats (amount > 0, currency, date...) are checked by CreateExpenseRequestValidator.
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        var expense = _mapper.Map<Expense>(request);
        expense.UserId = currentUserId;
        expense.Category = category;

        // NEW: for a recurring expense, the date of its first automatic copy.
        expense.ScheduleNextOccurrence(DateTime.UtcNow);

        // Spending BEFORE this expense - needed to detect whether this expense is the one
        // that crosses a budget threshold (and not every expense after it).
        var spentBefore = await _budgetAlerts.GetSpentInMonthAsync(
            currentUserId, expense.CategoryId, expense.Date, cancellationToken);

        await _unitOfWork.Expenses.AddAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _budgetAlerts.NotifyIfThresholdCrossedAsync(
            currentUserId, expense.CategoryId, category.Name, expense.Currency,
            expense.Date, spentBefore, expense.Amount, cancellationToken);

        return _mapper.Map<ExpenseResponse>(expense);
    }

    public async Task<ExpenseResponse> UpdateAsync(
        Guid expenseId,
        UpdateExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Трошак није пронађен.");

        if (expense.UserId != currentUserId)
            throw new ForbiddenException("Не можете уређивати туђи трошак.");

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        var previousSchedule = (expense.Date, expense.IsRecurring, expense.RecurrenceInterval);

        // Overwrites only the fields present in the request; Id and UserId stay untouched.
        _mapper.Map(request, expense);
        expense.Category = category;

        // NEW: the schedule is recalculated only if the date or the repetition changed. Editing
        // e.g. only the description must not move (or skip) the copy that is already due.
        if (previousSchedule != (expense.Date, expense.IsRecurring, expense.RecurrenceInterval))
            expense.ScheduleNextOccurrence(DateTime.UtcNow);

        _unitOfWork.Expenses.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Note: budget thresholds are checked only when an expense is created.

        return _mapper.Map<ExpenseResponse>(expense);
    }

    public async Task DeleteAsync(Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Трошак није пронађен.");

        if (expense.UserId != currentUserId)
            throw new ForbiddenException("Не можете обрисати туђи трошак.");

        // Deleting a recurring expense also stops its copies; copies already created stay.
        _unitOfWork.Expenses.Remove(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ---------- NEW ----------

    public async Task<ExpenseResponse> GetByIdAsync(
        Guid expenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var expense = await _unitOfWork.Expenses.GetWithCategoryAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Трошак није пронађен.");

        if (expense.UserId != currentUserId)
            throw new ForbiddenException("Немате приступ овом трошку.");

        return _mapper.Map<ExpenseResponse>(expense);
    }

    public async Task<PagedResponse<ExpenseResponse>> GetMyExpensesAsync(
        Guid currentUserId,
        ExpenseQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        // Value ranges are checked by ExpenseQueryParametersValidator.
        var filter = new ExpenseFilter
        {
            UserId = currentUserId,
            // "from=2026-10-01&to=2026-10-31" means both days included: the end becomes
            // the start of 1 November, exclusive. Dates are stored in UTC.
            From = query.From?.AsUtc().Date,
            ToExclusive = query.To?.AsUtc().Date.AddDays(1),
            CategoryId = query.CategoryId,
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            MinAmount = query.MinAmount,
            MaxAmount = query.MaxAmount,
            IsRecurring = query.IsRecurring,
            SortBy = query.SortBy,
            SortDescending = query.SortDescending,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var result = await _unitOfWork.Expenses.GetPagedByUserAsync(filter, cancellationToken);

        return new PagedResponse<ExpenseResponse>
        {
            Items = _mapper.Map<List<ExpenseResponse>>(result.Items),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
