namespace FairShare.WebAPI.Extensions
{
    public static class CorsServiceExtensions
    {
        public const string FrontendPolicy = "Frontend";

        /// <summary>
        /// Used when the "Cors:AllowedOrigins" section is missing: the Vite dev server of the React
        /// web app (5173) and the Expo dev server (8081) when the mobile app is run in a browser.
        /// Native mobile apps are not subject to CORS at all - it is a browser rule.
        /// </summary>
        private static readonly string[] DefaultOrigins =
        {
            "http://localhost:5173",
            "http://localhost:8081"
        };

        /// <summary>
        /// Allows the web frontend, which runs on a different origin (port), to call the API from
        /// the browser. Only the listed origins are allowed - never "any origin".
        /// </summary>
        public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration config)
        {
            var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (origins is null || origins.Length == 0)
                origins = DefaultOrigins;

            services.AddCors(options =>
            {
                options.AddPolicy(FrontendPolicy, policy => policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    // Lets the frontend read how long to wait after HTTP 429.
                    .WithExposedHeaders("Retry-After"));
                // No AllowCredentials(): the access token travels in the Authorization header and
                // the refresh token in the request body, so no cookies are involved.
            });

            return services;
        }
    }
}
