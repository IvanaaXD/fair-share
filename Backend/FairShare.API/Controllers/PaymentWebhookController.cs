using System.Text;
using System.Text.Json;
using FairShare.Api.Filters;
using FairShare.Application.Common.Payments;
using FairShare.Application.DTOs.Payments;
using FairShare.Application.Interfaces;
using FairShare.Infrastructure.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FairShare.API.Controllers;

/// <summary>
/// Results of card charges, sent by the payment simulator service. There is no user here, so
/// the request is authenticated by its HMAC signature instead of a JWT: without the shared
/// secret nobody can report "the payment succeeded".
/// </summary>
[ApiController]
[Route("api/webhooks/payments")]
[AllowAnonymous]
[SkipAudit]
public class PaymentWebhookController : ControllerBase
{
    /// <summary>How old a webhook may be (protection against replaying a captured request).</summary>
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IPaymentService _paymentService;
    private readonly PaymentSimulatorOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(
        IPaymentService paymentService,
        IOptions<PaymentSimulatorOptions> options,
        TimeProvider timeProvider,
        ILogger<PaymentWebhookController> logger)
    {
        _paymentService = paymentService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        // The signature covers the exact bytes that were sent, so the body is read as text
        // first and only then turned into an object.
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers[WebhookSignatureVerifier.HeaderName].ToString();

        if (!WebhookSignatureVerifier.IsValid(_options.WebhookSecret, body, signature, _timeProvider.GetUtcNow(), Tolerance))
        {
            _logger.LogWarning("Payment webhook with a missing or wrong signature was rejected.");
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid signature.");
        }

        PaymentWebhookRequest? webhook;
        try
        {
            webhook = JsonSerializer.Deserialize<PaymentWebhookRequest>(body, Json);
        }
        catch (JsonException)
        {
            webhook = null;
        }

        if (webhook is null || string.IsNullOrWhiteSpace(webhook.Id) || string.IsNullOrWhiteSpace(webhook.Data.Id))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Неисправан садржај.");

        var outcome = await _paymentService.HandleWebhookAsync(webhook, cancellationToken);
        _logger.LogInformation("Payment webhook {EventId} ({Type}) for charge {ChargeId}: {Outcome}",
            webhook.Id, webhook.Type, webhook.Data.Id, outcome);

        // 2xx tells the simulator to stop repeating the webhook - also for a duplicate.
        return Ok(new { received = true, outcome = outcome.ToString() });
    }
}
