using System.Text.Json.Serialization;
using FairShare.API.Hubs;
using FairShare.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;

namespace FairShare.WebAPI.Extensions;

/// <summary>Real-time notifications with SignalR: services, token from the address, the hub route.</summary>
public static class RealtimeServiceExtensions
{
    public static IServiceCollection AddRealtimeNotifications(this IServiceCollection services)
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

        return services;
    }

    public static IEndpointRouteBuilder MapRealtimeNotifications(this IEndpointRouteBuilder app)
    {
        app.MapHub<NotificationHub>(NotificationHub.Path);
        return app;
    }
}
