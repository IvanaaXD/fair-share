using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.WebAPI.Extensions
{
    /// <summary>Names of the rate limiting policies, used in [EnableRateLimiting(...)].</summary>
    public static class RateLimitPolicies
    {
        /// <summary>Sign-in, registration and everything that sends e-mails - strict, per IP address.</summary>
        public const string Authentication = "authentication";
    }

    /// <summary>Section "RateLimiting" of appsettings.json.</summary>
    public sealed class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        /// <summary>
        /// false only for load tests: every virtual user comes from the same IP address and a few
        /// accounts, so the limits would measure themselves instead of the API.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Global limit: generous, protects the API from a single client flooding it.</summary>
        public int PermitsPerMinute { get; set; } = 300;

        /// <summary>Strict: makes guessing passwords and spamming someone's inbox impractical.</summary>
        public int AuthenticationPermitsPerMinute { get; set; } = 10;
    }

    public static class RateLimitingServiceExtensions
    {
        /// <summary>
        /// Built-in ASP.NET Core rate limiting (no external service). Requests over the limit get
        /// HTTP 429 with a Retry-After header. Limits are kept in memory, per API instance.
        /// </summary>
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration config)
        {
            var settings = config.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                if (!settings.Enabled)
                {
                    // The policy name still has to exist, because controllers refer to it.
                    options.AddPolicy(RateLimitPolicies.Authentication, _ => RateLimitPartition.GetNoLimiter("off"));
                    return;
                }

                // Applies to every request: one bucket per signed-in user, or per IP address for
                // anonymous requests.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetUserOrIpKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.PermitsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Applies only to actions marked [EnableRateLimiting(RateLimitPolicies.Authentication)].
                // Always per IP address, because these requests come before the user is signed in.
                options.AddPolicy(RateLimitPolicies.Authentication, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetIpKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.AuthenticationPermitsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Same ProblemDetails format as every other error of the API.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var response = context.HttpContext.Response;

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds))
                            .ToString(CultureInfo.InvariantCulture);

                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "You have sent too many requests in a short time. Please try again in a minute.",
                        Instance = context.HttpContext.Request.Path
                    };

                    await response.WriteAsJsonAsync(
                        problem, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
                };
            });

            return services;
        }

        private static string GetUserOrIpKey(HttpContext httpContext)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId is not null ? $"user:{userId}" : GetIpKey(httpContext);
        }

        private static string GetIpKey(HttpContext httpContext)
            => $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
