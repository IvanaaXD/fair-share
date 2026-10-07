using FairShare.Application.Abstractions;
using FairShare.Application.Interfaces;
using FairShare.Application.Services;
using FairShare.Infrastructure.Email;

namespace FairShare.WebAPI.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IExpenseService, ExpenseService>();
            services.AddScoped<IGroupExpenseService, GroupExpenseService>();
            services.AddScoped<ISettlementService, SettlementService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IBudgetService, BudgetService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();

            services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
            services.AddSingleton<EmailQueue>();
            services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddHostedService<EmailBackgroundService>();

            return services;
        }
    }
}