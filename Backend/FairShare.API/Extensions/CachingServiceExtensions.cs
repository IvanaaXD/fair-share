using FairShare.Application.Abstractions;
using FairShare.Application.Interfaces;
using FairShare.Application.Services;
using FairShare.Application.Services.Caching;
using FairShare.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Hybrid;

namespace FairShare.WebAPI.Extensions
{
    public static class CachingServiceExtensions
    {
        /// <summary>
        /// Caching. Must be called AFTER AddApplicationServices, because it replaces the
        /// IAnalyticsService and IAdminStatisticsService registrations with cached decorators.
        /// </summary>
        public static IServiceCollection AddCaching(this IServiceCollection services)
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

            // Redis as a second cache level (shared by several API instances, survives restarts)
            // would be just this - no other code changes, HybridCache picks it up automatically:
            //   dotnet add FairShare.API package Microsoft.Extensions.Caching.StackExchangeRedis
            //   services.AddStackExchangeRedisCache(o => o.Configuration = config.GetConnectionString("Redis"));
            // Garnet (Microsoft's Redis-compatible server) works the same way without Docker.

            services.AddSingleton<ICacheService, HybridCacheService>();

            // Used by the DbContext registration in Program.cs.
            services.AddSingleton<CacheInvalidationInterceptor>();

            // Decorators: the original service does the work, the decorator adds caching in front.
            // The last registration of an interface wins, so these replace the plain ones.
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
}
