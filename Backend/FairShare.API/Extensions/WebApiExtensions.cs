using System.Text.Json.Serialization;
using FairShare.Api.Filters;
using FairShare.API.Hubs;
using FairShare.API.OpenApi;
using FairShare.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;

namespace FairShare.WebAPI.Extensions;

/// <summary>
/// Presentation layer: controllers and their filters, API documentation, CORS, running behind
/// Nginx and real-time notifications (SignalR). Rate limiting is in RateLimitingServiceExtensions,
/// authentication in IdentityServiceExtensions.
/// </summary>
public static class WebApiExtensions
{
    public const string FrontendCorsPolicy = "Frontend";

    /// <summary>
    /// Used when the "Cors:AllowedOrigins" section is missing: the Vite dev server of the React
    /// web app. Native mobile apps are not subject to CORS at all - it is a browser rule.
    /// </summary>
    private static readonly string[] DefaultOrigins = ["http://localhost:5173"];

    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration config)
    {
        AddControllersWithFilters(services);
        AddApiDocumentation(services);
        AddFrontendCors(services, config);
        AddRealtimeNotifications(services);

        if (config.IsBehindReverseProxy())
        {
            // Nginx terminates HTTPS and forwards the request: the client's address and the
            // original scheme come in X-Forwarded-* headers.
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
            });
        }

        return services;
    }

    /// <summary>"ReverseProxy:Enabled" - true in docker-compose, where Nginx is in front of the API.</summary>
    public static bool IsBehindReverseProxy(this IConfiguration config)
        => config.GetValue<bool>("ReverseProxy:Enabled");

    /// <summary>Swagger UI at /swagger; it only displays the document ASP.NET Core generates (/openapi/v1.json).</summary>
    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi().AllowAnonymous();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "FairShare API v1");
            options.DocumentTitle = "FairShare API";
        });
        return app;
    }

    private static void AddControllersWithFilters(IServiceCollection services)
    {
        // Order: account state first, then audit, then validation right before the action.
        services.AddScoped<RequirePasswordChangeFilter>();
        services.AddScoped<AuditLogFilter>();
        services.AddControllers(options =>
        {
            options.Filters.AddService<RequirePasswordChangeFilter>();
            options.Filters.AddService<AuditLogFilter>();
            options.Filters.Add<ValidationFilter>();
        })
        // Enums as text ("Admin", "Monthly") instead of numbers.
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    }

    private static void AddApiDocumentation(IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "FairShare API";
                document.Info.Version = "v1";
                document.Info.Description =
                    "REST API for tracking personal expenses, sharing group expenses, and settling debts.";
                return Task.CompletedTask;
            });

            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });
    }

    /// <summary>
    /// Allows the web frontend, which runs on a different origin (port), to call the API from
    /// the browser. Only the listed origins are allowed - never "any origin".
    /// </summary>
    private static void AddFrontendCors(IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is null || origins.Length == 0)
            origins = DefaultOrigins;

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                // Lets the frontend read how long to wait after HTTP 429.
                .WithExposedHeaders("Retry-After"));
            // No AllowCredentials(): the access token travels in the Authorization header and
            // the refresh token in the request body, so no cookies are involved.
        });
    }

    /// <summary>Real-time notifications with SignalR (route: NotificationHub.Path).</summary>
    private static void AddRealtimeNotifications(IServiceCollection services)
    {
        services
            .AddSignalR()
            // Enums as text ("BudgetExceeded"), exactly like the REST API.
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
        services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();

        // A browser cannot set the Authorization header on a WebSocket, so the SignalR client
        // sends the access token as ?access_token=... Accept it there, but only for the hub.
        // PostConfigure adds this to the existing JWT setup (AddIdentityServices) without changing it.
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events ??= new JwtBearerEvents();
            var previous = options.Events.OnMessageReceived;
            options.Events.OnMessageReceived = async context =>
            {
                await previous(context);

                var accessToken = context.Request.Query["access_token"];
                if (string.IsNullOrEmpty(context.Token) &&
                    !string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments(NotificationHub.Path))
                {
                    context.Token = accessToken;
                }
            };
        });
    }
}
