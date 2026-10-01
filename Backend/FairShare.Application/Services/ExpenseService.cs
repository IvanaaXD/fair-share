using FairShare.Application.DTOs.Expenses;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExpenseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new ConflictException("Износ трошка мора бити већи од нуле.");

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        var expense = new Expense
        {
            UserId = currentUserId,
            CategoryId = request.CategoryId,
            Category = category,
            Amount = request.Amount,
            Currency = request.Currency,
            Date = request.Date,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ReceiptImageUrl = request.ReceiptImageUrl,
            IsRecurring = request.IsRecurring,
            RecurrenceInterval = request.RecurrenceInterval
        };

        await _unitOfWork.Expenses.AddAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(expense);
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

        expense.CategoryId = request.CategoryId;
        expense.Category = category;
        expense.Amount = request.Amount;
        expense.Currency = request.Currency;
        expense.Date = request.Date;
        expense.Description = request.Description;
        expense.Latitude = request.Latitude;
        expense.Longitude = request.Longitude;
        expense.ReceiptImageUrl = request.ReceiptImageUrl;
        expense.IsRecurring = request.IsRecurring;
        expense.RecurrenceInterval = request.RecurrenceInterval;

        _unitOfWork.Expenses.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(expense);
    }

    public async Task DeleteAsync(Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Трошак није пронађен.");

        if (expense.UserId != currentUserId)
            throw new ForbiddenException("Не можете обрисати туђи трошак.");

        _unitOfWork.Expenses.Remove(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseResponse>> GetMyExpensesAsync(
        Guid currentUserId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var expenses = await _unitOfWork.Expenses.GetByUserAsync(currentUserId, from, to, categoryId, cancellationToken);
        return expenses.Select(MapToResponse).ToList();
    }

    private static ExpenseResponse MapToResponse(Expense expense) => new()
    {
        Id = expense.Id,
        CategoryId = expense.CategoryId,
        CategoryName = expense.Category?.Name ?? string.Empty,
        Amount = expense.Amount,
        Currency = expense.Currency,
        Date = expense.Date,
        Description = expense.Description,
        Latitude = expense.Latitude,
        Longitude = expense.Longitude,
        ReceiptImageUrl = expense.ReceiptImageUrl,
        IsRecurring = expense.IsRecurring,
        RecurrenceInterval = expense.RecurrenceInterval
    };
}
