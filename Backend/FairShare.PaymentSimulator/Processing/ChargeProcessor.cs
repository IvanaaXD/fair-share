using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FairShare.PaymentSimulator.Data;
using FairShare.PaymentSimulator.Domain;
using FairShare.PaymentSimulator.Endpoints;
using FairShare.PaymentSimulator.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FairShare.PaymentSimulator.Processing;

/// <summary>
/// "The bank": every second it
///   1. completes charges that have waited ProcessingDelaySeconds (success or the test card's error),
///   2. reports completed charges to FairShare with a signed webhook, retrying with growing
///      pauses until FairShare answers 2xx.
/// Everything it needs is in the database, so a restart of the simulator loses nothing.
/// </summary>
public sealed class ChargeProcessor : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<SimulatorOptions> _options;
    private readonly TimeProvider _time;
    private readonly ILogger<ChargeProcessor> _logger;

    public ChargeProcessor(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<SimulatorOptions> options,
        TimeProvider time,
        ILogger<ChargeProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Tick);
        do
        {
            try
            {
                await CompleteDueChargesAsync(stoppingToken);
                await DeliverWebhooksAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One failed round (e.g. the database restarting) must not stop the processor.
                _logger.LogError(ex, "Charge processing round failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CompleteDueChargesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimulatorDbContext>();

        var now = _time.GetUtcNow().UtcDateTime;
        var dueBefore = now.AddSeconds(-_options.CurrentValue.ProcessingDelaySeconds);

        var due = await db.Charges
            .Include(c => c.CardToken)
            .Where(c => c.Status == ChargeStatus.Pending && c.CreatedAt <= dueBefore)
            .OrderBy(c => c.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
            return;

        foreach (var charge in due)
        {
            charge.Complete(now);
            _logger.LogInformation("Charge {ChargeId} {Status}{Code}", charge.Id, charge.Status,
                charge.FailureCode is null ? string.Empty : $" ({charge.FailureCode})");
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeliverWebhooksAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.WebhookUrl))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimulatorDbContext>();

        var now = _time.GetUtcNow().UtcDateTime;
        var pending = await db.Charges
            .Include(c => c.CardToken)
            .Where(c => c.Status != ChargeStatus.Pending
                        && c.WebhookDeliveredAt == null
                        && c.WebhookAttempts < options.MaxWebhookAttempts
                        && c.NextWebhookAttemptAt <= now)
            .OrderBy(c => c.NextWebhookAttemptAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var charge in pending)
        {
            var error = await SendAsync(charge, options, cancellationToken);
            var sentAt = _time.GetUtcNow().UtcDateTime;

            if (error is null)
            {
                charge.WebhookDelivered(sentAt);
                _logger.LogInformation("Webhook {EventId} for charge {ChargeId} delivered.", charge.EventId, charge.Id);
            }
            else
            {
                charge.WebhookFailed(sentAt, error);
                _logger.LogWarning("Webhook {EventId} for charge {ChargeId} failed (attempt {Attempt}): {Error}. Next try at {Next:O}.",
                    charge.EventId, charge.Id, charge.WebhookAttempts, error, charge.NextWebhookAttemptAt);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>Sends one webhook; returns null on success or a short description of the failure.</summary>
    private async Task<string?> SendAsync(Charge charge, SimulatorOptions options, CancellationToken cancellationToken)
    {
        var payload = new
        {
            id = charge.EventId,
            type = charge.Status == ChargeStatus.Succeeded ? "charge.succeeded" : "charge.failed",
            createdAt = charge.CompletedAt,
            data = ChargeResponse.From(charge)
        };

        var body = JsonSerializer.Serialize(payload, Json);
        using var request = new HttpRequestMessage(HttpMethod.Post, options.WebhookUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add(WebhookSignature.HeaderName, WebhookSignature.Create(options.WebhookSecret, body, _time.GetUtcNow()));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var client = _httpClientFactory.CreateClient("webhooks");
            using var response = await client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // FairShare is down or slow - the webhook is tried again later.
            return ex.Message;
        }
    }
}
