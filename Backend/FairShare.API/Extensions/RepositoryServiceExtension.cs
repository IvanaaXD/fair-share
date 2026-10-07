using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FairShare.WebAPI.Extensions
{
    public static class RepositoryServiceExtensions
    {
        public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IExpenseRepository, ExpenseRepository>();
            services.AddScoped<IBudgetRepository, BudgetRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
            services.AddScoped<IGroupExpenseRepository, GroupExpenseRepository>();
            services.AddScoped<IExpenseSplitRepository, ExpenseSplitRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<ISettlementTransactionRepository, SettlementTransactionRepository>();
            services.AddScoped<IQrPaymentDataRepository, QrPaymentDataRepository>();
            services.AddScoped<IPaymentCardRepository, PaymentCardRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}