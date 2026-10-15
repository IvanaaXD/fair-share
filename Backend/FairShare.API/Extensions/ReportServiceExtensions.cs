using FairShare.Application.Abstractions;
using FairShare.Application.Interfaces;
using FairShare.Application.Services;
using FairShare.Infrastructure.Reports;

namespace FairShare.WebAPI.Extensions;

public static class ReportServiceExtensions
{
    /// <summary>PDF and CSV reports. Call after AddExchangeRates and AddCaching.</summary>
    public static IServiceCollection AddReports(this IServiceCollection services)
    {
        // The renderer has no state of its own (QuestPDF is configured once), so one instance is enough.
        services.AddSingleton<IPdfReportRenderer, QuestPdfReportRenderer>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
