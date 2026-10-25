using FairShare.Application.Abstractions;
using FairShare.Application.Interfaces;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.BackgroundJobs;
using FairShare.Infrastructure.Caching;
using FairShare.Infrastructure.Currency;
using FairShare.Infrastructure.Data;
using FairShare.Infrastructure.Email;
using FairShare.Infrastructure.Payments;
using FairShare.Infrastructure.Qr;
using FairShare.Infrastructure.Reports;
using FairShare.Infrastructure.Repositories;
using FairShare.Infrastructure.Seed;
using FairShare.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace FairShare.WebAPI.Extensions;

/// <summary>
/// Infrastructure layer: the database, repositories, and everything that talks to the outside
/// world (e-mail, exchange rates, the payment simulator, files, cache). These are the
/// implementations of the interfaces the Application layer depends on.
/// </summary>
public static class InfrastructureLayerExtensions
{
    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        // The system clock; a test can register a fake TimeProvider and move time forward.
        services.AddSingleton(TimeProvider.System);

        AddDatabase(services, config);
        AddRepositories(services);
        AddCache(services, config);
        AddEmail(services, config);
        AddExchangeRates(services);
        AddPaymentSimulator(services);

        // Uploaded images (profile images, receipt photos).
        services.AddFileStorage(config, environment);

        // No state of their own, so one instance is enough.
        services.AddSingleton<IQrCodeImageGenerator, QrCoderImageGenerator>();
        services.AddSingleton<IPdfReportRenderer, QuestPdfReportRenderer>();

        // Automatic copies of recurring expenses.
        services.AddHostedService<RecurringExpenseBackgroundService>();

        return services;
    }

    /// <summary>
    /// Applies migrations (only when configured - in a container there is no SDK to do it) and
    /// creates the administrator and, optionally, the demo data.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<FairShareDbContext>().Database.MigrateAsync();
        }

        await AdminUserSeeder.SeedAsync(app.Services);

        // The basic categories, only on an empty database (a new installation, e.g. in Docker).
        await CategorySeeder.SeedAsync(app.Services);

        if (app.Configuration.GetValue<bool>("DemoData:Enabled"))
            await DemoDataSeeder.SeedAsync(app.Services);
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");

        // The interceptors run on every SaveChanges: one deletes files of removed images, the
        // other removes cache entries whose data has changed.
        services.AddDbContext<FairShareDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<FileCleanupInterceptor>(),
                    serviceProvider.GetRequiredService<CacheInvalidationInterceptor>()));
    }

    private static void AddRepositories(IServiceCollection services)
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
        services.AddScoped<IStatisticsRepository, StatisticsRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddCache(IServiceCollection services, IConfiguration config)
    {
        services.AddHybridCache(options =>
        {
            // Used only by entries that do not set their own duration (all of ours do).
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });

        // Redis as the second cache level (L2): shared by all API instances and kept when the API
        // restarts. HybridCache uses any registered IDistributedCache on its own, so no other code
        // changes. Without "ConnectionStrings:Redis" (local development) only the in-memory level is used.
        var redis = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redis;
                options.InstanceName = "fairshare:";
            });
        }

        // "Caching:Enabled": false only for the load test that measures the cache - the same
        // requests are then answered from the database every time.
        if (config.GetValue("Caching:Enabled", true))
            services.AddSingleton<ICacheService, HybridCacheService>();
        else
            services.AddSingleton<ICacheService, NoCacheService>();

        services.AddSingleton<CacheInvalidationInterceptor>();
    }

    private static void AddEmail(IServiceCollection services, IConfiguration config)
    {
        // E-mails are put in a queue and sent in the background, so a request never waits for SMTP.
        services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddHostedService<EmailBackgroundService>();
    }

    private static void AddExchangeRates(IServiceCollection services)
    {
        // A short timeout: if the rate service is slow, the app uses the last known rates
        // instead of making the user wait.
        services.AddHttpClient<IExchangeRateProvider, ExchangeRateApiProvider>(client =>
            client.Timeout = TimeSpan.FromSeconds(5));
    }

    private static void AddPaymentSimulator(IServiceCollection services)
    {
        // The API does not start without the simulator's address and both keys (user-secrets) -
        // better than failing on the first payment.
        services.AddOptions<PaymentSimulatorOptions>()
            .BindConfiguration(PaymentSimulatorOptions.SectionName)
            .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "PaymentSimulator:BaseUrl must be an absolute URL.")
            .Validate(o => o.ApiKey.Length >= 32, "PaymentSimulator:ApiKey must be set (at least 32 characters, user-secrets).")
            .Validate(o => o.WebhookSecret.Length >= 32, "PaymentSimulator:WebhookSecret must be set (at least 32 characters, user-secrets).")
            .ValidateOnStart();

        services.AddHttpClient<IPaymentGateway, PaymentSimulatorClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PaymentSimulatorOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        });
    }
}
