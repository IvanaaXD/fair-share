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

    public static class RateLimitingServiceExtensions
    {
        // Global limit: generous, protects the API from a single client flooding it.
        private const int GlobalPermitsPerMinute = 300;

        // Authentication limit: strict, makes guessing passwords and spamming someone's inbox impractical.
        private const int AuthenticationPermitsPerMinute = 10;

        /// <summary>
        /// Built-in ASP.NET Core rate limiting (no external service). Requests over the limit get
        /// HTTP 429 with a Retry-After header. Limits are kept in memory, per API instance.
        /// </summary>
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Applies to every request: one bucket per signed-in user, or per IP address for
                // anonymous requests.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetUserOrIpKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = GlobalPermitsPerMinute,
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
                            PermitLimit = AuthenticationPermitsPerMinute,
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
                        Title = "Превише захтјева.",
                        Detail = "Послали сте превише захтјева у кратком времену. Покушајте поново за минут.",
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
