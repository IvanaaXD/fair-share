using FairShare.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FairShare.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs once when the API starts and then every hour, and adds the copies of recurring expenses
/// that have become due. Running every hour (not once a day at a fixed time) means a copy is
/// created at most an hour late, and nothing is missed if the API was off at that moment.
///
/// Each expense is processed in its own DI scope (its own DbContext), so one failing expense
/// cannot block or corrupt the others.
/// </summary>
public sealed class RecurringExpenseBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RecurringExpenseBackgroundService> _logger;

    public RecurringExpenseBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<RecurringExpenseBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval, _timeProvider);

        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The API is shutting down.
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        // An exception that escapes ExecuteAsync stops the whole API, so every error is caught
        // and logged here; the next run simply tries again.
        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            IReadOnlyList<Guid> dueIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                dueIds = await scope.ServiceProvider
                    .GetRequiredService<IRecurringExpenseService>()
                    .GetDueExpenseIdsAsync(nowUtc, stoppingToken);
            }

            var created = 0;
            foreach (var expenseId in dueIds)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    created += await scope.ServiceProvider
                        .GetRequiredService<IRecurringExpenseService>()
                        .GenerateDueOccurrencesAsync(expenseId, nowUtc, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Recurring expense {ExpenseId} could not be processed.", expenseId);
                }
            }

            if (created > 0)
                _logger.LogInformation("Added {Count} recurring expense copies.", created);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Recurring expenses check failed.");
        }
    }
}
