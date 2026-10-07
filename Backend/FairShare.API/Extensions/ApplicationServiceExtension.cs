using FairShare.Application.Abstractions;
using FairShare.Application.Interfaces;
using FairShare.Application.Mappings;
using FairShare.Application.Services;
using FairShare.Application.Validators;
using FairShare.Infrastructure.Email;
using FairShare.Infrastructure.Qr;
using FluentValidation;

namespace FairShare.WebAPI.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // ---------- AutoMapper + FluentValidation ----------
            // Scans the Application assembly for every Profile and every AbstractValidator<T>.
            services.AddAutoMapper(typeof(GroupMappingProfile).Assembly);
            services.AddValidatorsFromAssemblyContaining<CreateGroupRequestValidator>();

            // ---------- business logic ----------
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

            // profile and QR payments
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IQrPaymentService, QrPaymentService>();
            services.AddSingleton<IQrCodeImageGenerator, QrCoderImageGenerator>();

            // audit log and admin statistics
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IAdminStatisticsService, AdminStatisticsService>();

            // ---------- e-mail (queue + background sender) ----------
            services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
            services.AddSingleton<EmailQueue>();
            services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddHostedService<EmailBackgroundService>();

            return services;
        }
    }
}
