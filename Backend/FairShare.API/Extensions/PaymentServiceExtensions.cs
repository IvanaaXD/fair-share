using FairShare.Application.Abstractions;
using FairShare.Application.Interfaces;
using FairShare.Application.Services;
using FairShare.Infrastructure.Payments;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FairShare.WebAPI.Extensions;

public static class PaymentServiceExtensions
{
    /// <summary>
    /// Card payments: saved cards, paying settlements, and the HTTP client of the separate
    /// payment simulator service. Settings: section "PaymentSimulator" (keys in user-secrets).
    /// </summary>
    public static IServiceCollection AddPayments(this IServiceCollection services)
    {
        // Already registered if the recurring-expense step added it; TryAdd does not add it twice.
        services.TryAddSingleton(TimeProvider.System);

        // The API does not start without the simulator's address and both keys - better than
        // failing on the first payment.
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

        services.AddScoped<IPaymentCardService, PaymentCardService>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
