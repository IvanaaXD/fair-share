using FairShare.Application.Abstractions;
using FairShare.Application.Interfaces;
using FairShare.Application.Mappings;
using FairShare.Application.Services;
using FairShare.Application.Services.Caching;
using FairShare.Application.Validators;
using FluentValidation;

namespace FairShare.WebAPI.Extensions;

/// <summary>
/// Application layer: business logic (services), mapping and validation. Only interfaces from
/// the Application and Domain layers are used here; their implementations come from
/// <see cref="InfrastructureLayerExtensions"/>.
/// </summary>
public static class ApplicationLayerExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        // Scans the Application assembly for every Profile and every AbstractValidator<T>.
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(GroupMappingProfile).Assembly));
        services.AddValidatorsFromAssemblyContaining<CreateGroupRequestValidator>();

        // ---------- personal expenses, budgets, analytics, reports ----------
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IRecurringExpenseService, RecurringExpenseService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IBudgetAlertService, BudgetAlertService>();
        services.AddScoped<IReportService, ReportService>();

        // ---------- groups, group expenses, settling debts ----------
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IGroupExpenseService, GroupExpenseService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IQrPaymentService, QrPaymentService>();
        services.AddScoped<IPaymentCardService, PaymentCardService>();
        services.AddScoped<IPaymentService, PaymentService>();

        // ---------- users, notifications, administration ----------
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // ---------- cached services (decorator pattern) ----------
        // The original service does the work; the decorator in front of it answers repeated
        // requests from the cache. Controllers ask for the interface and get the decorator.
        services.AddScoped<AnalyticsService>();
        services.AddScoped<IAnalyticsService>(sp => new CachedAnalyticsService(
            sp.GetRequiredService<AnalyticsService>(),
            sp.GetRequiredService<ICacheService>()));

        services.AddScoped<AdminStatisticsService>();
        services.AddScoped<IAdminStatisticsService>(sp => new CachedAdminStatisticsService(
            sp.GetRequiredService<AdminStatisticsService>(),
            sp.GetRequiredService<ICacheService>()));

        return services;
    }
}
