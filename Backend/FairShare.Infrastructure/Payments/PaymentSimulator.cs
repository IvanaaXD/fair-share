using System.Collections.Concurrent;
using System.Security.Cryptography;
using FairShare.Application.Abstractions;
using FairShare.Domain.Services;
using Microsoft.Extensions.Logging;

namespace FairShare.Infrastructure.Payments;

/// <summary>
/// Payment simulator - stands in for a real card provider (bank / payment gateway), so card
/// payments can be shown end to end without real money.
///
/// It behaves like real providers in the parts that matter for the application:
/// - a card is exchanged for a token; the number and CVV are not kept anywhere;
/// - every charge takes a moment (network + bank);
/// - the same idempotency key always returns the first result, so nothing is charged twice;
/// - well-known test card numbers give the declined outcomes:
///     4000 0000 0000 0002  -> card_declined
///     4000 0000 0000 9995  -> insufficient_funds
///     4000 0000 0000 0069  -> expired_card
///     4000 0000 0000 0119  -> processing_error
///   every other valid card number (e.g. 4242 4242 4242 4242) is charged successfully.
///
/// The outcome is written into the token itself ("sim_ok_...", "sim_insufficient_funds_..."),
/// so saved cards keep working after the API restarts, without a database of the simulator.
/// </summary>
public sealed class PaymentSimulator : IPaymentGateway
{
    private const string TokenPrefix = "sim_";
    private const string Success = "ok";

    private static readonly Dictionary<string, string> TestCards = new()
    {
        ["4000000000000002"] = PaymentErrorCodes.CardDeclined,
        ["4000000000009995"] = PaymentErrorCodes.InsufficientFunds,
        ["4000000000000069"] = PaymentErrorCodes.ExpiredCard,
        ["4000000000000119"] = PaymentErrorCodes.ProcessingError,
    };

    // Results of charges by idempotency key (in a real provider this is their database).
    private readonly ConcurrentDictionary<string, ChargeResult> _charges = new();
    private readonly ILogger<PaymentSimulator> _logger;
    private readonly TimeSpan _delay;

    public PaymentSimulator(ILogger<PaymentSimulator> logger, TimeSpan? delay = null)
    {
        _logger = logger;
        _delay = delay ?? TimeSpan.FromMilliseconds(900);
    }

    public Task<CardTokenizationResult> TokenizeCardAsync(CardDetails card, CancellationToken cancellationToken = default)
    {
        var number = CardNumber.Normalize(card.Number);
        if (!CardNumber.IsValid(number) || card.Cvv.Length is < 3 or > 4)
            return Task.FromResult(new CardTokenizationResult(false, null, null, null, PaymentErrorCodes.InvalidCard));

        var outcome = TestCards.GetValueOrDefault(number, Success);
        var token = $"{TokenPrefix}{outcome}_{Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant()}";

        // Only the last four digits are ever logged.
        _logger.LogInformation("Simulator: card ****{Last4} tokenized", number[^4..]);

        return Task.FromResult(new CardTokenizationResult(true, token, CardNumber.DetectBrand(number), number[^4..], null));
    }

    public async Task<ChargeResult> ChargeAsync(
        string cardToken,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (_charges.TryGetValue(idempotencyKey, out var earlier))
            return earlier;

        // Network and bank.
        await Task.Delay(_delay, cancellationToken);

        ChargeResult result;
        if (!cardToken.StartsWith(TokenPrefix, StringComparison.Ordinal) || amount <= 0)
        {
            result = new ChargeResult(false, null, PaymentErrorCodes.InvalidCard);
        }
        else
        {
            var outcome = cardToken[TokenPrefix.Length..cardToken.LastIndexOf('_')];
            result = outcome == Success
                ? new ChargeResult(true, $"sim_txn_{Guid.NewGuid():N}", null)
                : new ChargeResult(false, null, outcome);
        }

        // Two requests with the same key at the same moment: both get the first stored result.
        result = _charges.GetOrAdd(idempotencyKey, result);
        _logger.LogInformation(
            "Simulator: charge {Amount} {Currency} -> {Result}",
            amount, currency, result.Success ? "succeeded" : result.ErrorCode);

        return result;
    }
}
