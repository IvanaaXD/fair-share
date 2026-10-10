using FairShare.Application.Interfaces;
using FairShare.Application.Services;
using FairShare.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FairShare.WebAPI.Extensions
{
    public static class RecurringExpenseServiceExtensions
    {
        /// <summary>Budget notifications and automatic copies of recurring expenses.</summary>
        public static IServiceCollection AddRecurringExpenses(this IServiceCollection services)
        {
            // Needed by ExpenseService as well - without this line every expense request fails.
            services.AddScoped<IBudgetAlertService, BudgetAlertService>();

            services.AddScoped<IRecurringExpenseService, RecurringExpenseService>();

            // The system clock; a test can register a fake TimeProvider and move time forward.
            services.TryAddSingleton(TimeProvider.System);
            services.AddHostedService<RecurringExpenseBackgroundService>();

            return services;
        }
    }
}
