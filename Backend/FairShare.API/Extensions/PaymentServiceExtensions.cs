using FairShare.Application.Abstractions;
using FairShare.Application.Interfaces;
using FairShare.Application.Services;
using FairShare.Infrastructure.Payments;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FairShare.WebAPI.Extensions;

public static class PaymentServiceExtensions
{
    /// <summary>Card payments: saved cards, paying settlements and the payment simulator.</summary>
    public static IServiceCollection AddPayments(this IServiceCollection services)
    {
        // Already registered if the recurring-expense step added it; TryAdd does not add it twice.
        services.TryAddSingleton(TimeProvider.System);

        // Singleton: the simulator remembers the results of charges by idempotency key.
        services.AddSingleton<IPaymentGateway>(sp =>
            new PaymentSimulator(sp.GetRequiredService<ILogger<PaymentSimulator>>()));

        services.AddScoped<IPaymentCardService, PaymentCardService>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
