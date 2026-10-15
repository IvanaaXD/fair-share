using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace FairShare.Infrastructure.Payments;

/// <summary>
/// HTTP client of the payment simulator service (FairShare.PaymentSimulator). The base
/// address, the timeout and the X-Api-Key header are set in PaymentServiceExtensions.
/// Card numbers and CVV codes are only passed through to the simulator - never logged.
/// </summary>
public sealed class PaymentSimulatorClient : IPaymentGateway
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<PaymentSimulatorClient> _logger;

    public PaymentSimulatorClient(HttpClient http, ILogger<PaymentSimulatorClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<CardTokenizationResult> TokenizeCardAsync(CardDetails card, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            number = card.Number,
            expiryMonth = card.ExpiryMonth,
            expiryYear = card.ExpiryYear,
            cvv = card.Cvv,
            holderName = card.HolderName
        };

        using var response = await SendAsync(
            () => _http.PostAsJsonAsync("v1/tokens", body, Json, cancellationToken), "tokenize", cancellationToken);

        if (response is null || (int)response.StatusCode >= 500)
            throw Unavailable();

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            return new CardTokenizationResult(false, null, null, null, error ?? PaymentErrorCodes.InvalidCard);
        }

        var token = await response.Content.ReadFromJsonAsync<TokenDto>(Json, cancellationToken);
        return token is null
            ? throw Unavailable()
            : new CardTokenizationResult(true, token.Token, token.Brand, token.Last4Digits, null);
    }

    public async Task<ChargeResult> ChargeAsync(GatewayChargeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(() =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "v1/charges")
            {
                Content = JsonContent.Create(new
                {
                    token = request.CardToken,
                    amount = request.Amount,
                    currency = request.Currency,
                    reference = request.Reference
                }, options: Json)
            };
            message.Headers.Add(IdempotencyHeader, request.IdempotencyKey);
            return _http.SendAsync(message, cancellationToken);
        }, "charge", cancellationToken);

        // No answer: it is not known whether the charge was created. The payment is marked
        // "gateway_unavailable"; a webhook that arrives later can still complete it.
        if (response is null || (int)response.StatusCode >= 500)
            return new ChargeResult(false, null, PaymentErrorCodes.GatewayUnavailable);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            _logger.LogWarning("Payment simulator refused a charge: {Status} {Error}", (int)response.StatusCode, error);
            return new ChargeResult(false, null, error == PaymentErrorCodes.InvalidCard ? error : PaymentErrorCodes.ProcessingError);
        }

        var charge = await response.Content.ReadFromJsonAsync<ChargeDto>(Json, cancellationToken);
        return new ChargeResult(charge is not null, charge?.Id, charge is null ? PaymentErrorCodes.ProcessingError : null);
    }

    public Task<ChargeStatusResult?> GetChargeAsync(string chargeId, CancellationToken cancellationToken = default)
        => GetStatusAsync($"v1/charges/{Uri.EscapeDataString(chargeId)}", cancellationToken);

    public Task<ChargeStatusResult?> FindChargeAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        => GetStatusAsync($"v1/charges?idempotencyKey={Uri.EscapeDataString(idempotencyKey)}", cancellationToken);

    // ---------- helpers ----------

    private async Task<ChargeStatusResult?> GetStatusAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(() => _http.GetAsync(path, cancellationToken), "status", cancellationToken);

        if (response is null || (int)response.StatusCode >= 500)
            throw Unavailable();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw Unavailable();

        var charge = await response.Content.ReadFromJsonAsync<ChargeDto>(Json, cancellationToken) ?? throw Unavailable();
        var outcome = charge.Status switch
        {
            "succeeded" => ChargeOutcome.Succeeded,
            "failed" => ChargeOutcome.Failed,
            _ => ChargeOutcome.Pending
        };
        return new ChargeStatusResult(charge.Id, outcome, charge.FailureCode);
    }

    /// <summary>Sends a request; null when the simulator cannot be reached or does not answer in time.</summary>
    private async Task<HttpResponseMessage?> SendAsync(
        Func<Task<HttpResponseMessage>> send, string operation, CancellationToken cancellationToken)
    {
        try
        {
            return await send();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Payment simulator unreachable ({Operation}): {Message}", operation, ex.Message);
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Payment simulator did not answer in time ({Operation}).", operation);
            return null;
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return (await response.Content.ReadFromJsonAsync<ErrorDto>(Json, cancellationToken))?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ServiceUnavailableException Unavailable()
        => new("Сервис за плаћање тренутно није доступан. Покушајте поново за минут.", ErrorCodes.PaymentServiceUnavailable);

    private sealed record TokenDto(string Token, string Brand, string Last4Digits);

    private sealed record ChargeDto(string Id, string Status, string? FailureCode);

    private sealed record ErrorDto(string? Error, string? Message);
}
