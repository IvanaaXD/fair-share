using System.Globalization;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

public class RecurringExpenseService : IRecurringExpenseService
{
    /// <summary>
    /// Upper bound of copies per expense in one run. If the server was off for a long time, a
    /// daily expense is caught up gradually over several runs instead of all at once.
    /// </summary>
    private const int MaxOccurrencesPerRun = 31;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IBudgetAlertService _budgetAlerts;
    private readonly INotificationService _notificationService;

    public RecurringExpenseService(
        IUnitOfWork unitOfWork,
        IBudgetAlertService budgetAlerts,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _budgetAlerts = budgetAlerts;
        _notificationService = notificationService;
    }

    public Task<IReadOnlyList<Guid>> GetDueExpenseIdsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        => _unitOfWork.Expenses.GetRecurringDueIdsAsync(nowUtc, cancellationToken);

    public async Task<int> GenerateDueOccurrencesAsync(
        Guid expenseId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        // Loaded again here (tracked): between the list query and now the user may have changed
        // or deleted the expense, or stopped the repetition.
        var template = await _unitOfWork.Expenses.GetByIdAsync(expenseId, cancellationToken);
        if (template is null || !template.IsRecurring)
            return 0;

        // Recurring expenses created before this feature have no date yet: only schedule them.
        if (template.NextOccurrenceDate is null)
        {
            template.ScheduleNextOccurrence(nowUtc);
            _unitOfWork.Expenses.Update(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(template.CategoryId, cancellationToken);
        var categoryName = category?.Name ?? string.Empty;

        var created = 0;
        while (template.IsOccurrenceDue(nowUtc) && created < MaxOccurrencesPerRun)
        {
            var spentBefore = await _budgetAlerts.GetSpentInMonthAsync(
                template.UserId, template.CategoryId, template.NextOccurrenceDate!.Value, cancellationToken);

            var occurrence = template.CreateNextOccurrence();
            await _unitOfWork.Expenses.AddAsync(occurrence, cancellationToken);
            _unitOfWork.Expenses.Update(template);

            // The copy and the moved date are saved in ONE transaction, so a copy can never be
            // created twice (e.g. if the server stops between two steps).
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            created++;

            await _notificationService.NotifyAsync(
                template.UserId,
                NotificationType.RecurringExpenseCreated,
                "Додат понављајући трошак",
                $"Аутоматски је додат трошак '{template.Description ?? categoryName}' " +
                $"од {occurrence.Amount} {occurrence.Currency} " +
                $"за {occurrence.Date.ToString("dd.MM.yyyy.", CultureInfo.InvariantCulture)}",
                cancellationToken);

            await _budgetAlerts.NotifyIfThresholdCrossedAsync(
                template.UserId, template.CategoryId, categoryName, occurrence.Currency,
                occurrence.Date, spentBefore, occurrence.Amount, cancellationToken);
        }

        return created;
    }
}
