using System.Text.RegularExpressions;
using FairShare.PaymentSimulator.Data;
using FairShare.PaymentSimulator.Domain;
using FairShare.PaymentSimulator.Security;
using Microsoft.EntityFrameworkCore;

namespace FairShare.PaymentSimulator.Endpoints;

/// <summary>
/// The provider's API, as FairShare sees it:
///   POST /v1/tokens          card -> token (the number and CVV are never stored)
///   POST /v1/charges         charge a token; answered at once with "pending" (202),
///                            the result comes later as a signed webhook
///   GET  /v1/charges/{id}    current state of a charge (if a webhook was missed)
///   GET  /v1/charges?idempotencyKey=...   the charge created with that key, if any
/// </summary>
public static partial class SimulatorEndpoints
{
    public const string IdempotencyHeader = "Idempotency-Key";
    private const decimal MaxAmount = 1_000_000m;

    public static void MapSimulatorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var api = app.MapGroup("/v1").AddEndpointFilter<ApiKeyFilter>();
        api.MapPost("/tokens", TokenizeAsync);
        api.MapPost("/charges", CreateChargeAsync);
        api.MapGet("/charges/{id}", GetChargeAsync);
        api.MapGet("/charges", FindChargeAsync);
    }

    private static async Task<IResult> TokenizeAsync(
        TokenizeRequest request,
        SimulatorDbContext db,
        TimeProvider time,
        ILogger<TokenizeRequest> logger,
        CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var number = TestCards.Normalize(request.Number);
        var cvv = request.Cvv ?? string.Empty;
        var holder = request.HolderName?.Trim();

        var card = new CardToken
        {
            Id = Guid.NewGuid(),
            Token = Ids.New("tok"),
            Last4Digits = number.Length >= 4 ? number[^4..] : number,
            Brand = number.Length > 0 ? TestCards.BrandOf(number) : "Card",
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            HolderName = string.IsNullOrEmpty(holder) ? null : holder[..Math.Min(holder.Length, 100)],
            Outcome = TestCards.OutcomeOf(number),
            CreatedAt = now
        };

        var valid = TestCards.IsValidNumber(number)
                    && request.ExpiryMonth is >= 1 and <= 12
                    && request.ExpiryYear is >= 2000 and <= 2100
                    && CvvPattern().IsMatch(cvv)
                    && !card.IsExpiredAt(now);

        if (!valid)
            return Results.BadRequest(new ErrorResponse(ChargeFailureCodes.InvalidCard,
                "The card number, the expiry date or the CVV is not valid."));

        db.CardTokens.Add(card);
        await db.SaveChangesAsync(cancellationToken);

        // Only the last four digits are ever written to the log.
        logger.LogInformation("Card tokenized: {Brand} **** {Last4}", card.Brand, card.Last4Digits);

        return Results.Created($"/v1/tokens/{card.Token}",
            new TokenResponse(card.Token, card.Brand, card.Last4Digits, card.ExpiryMonth, card.ExpiryYear));
    }

    private static async Task<IResult> CreateChargeAsync(
        ChargeRequest request,
        HttpRequest http,
        SimulatorDbContext db,
        TimeProvider time,
        ILogger<ChargeRequest> logger,
        CancellationToken cancellationToken)
    {
        var key = http.Headers[IdempotencyHeader].ToString().Trim();
        if (key.Length is 0 or > 100)
            return Results.BadRequest(new ErrorResponse("idempotency_key_required",
                $"Send a unique {IdempotencyHeader} header (at most 100 characters) with every charge."));

        var currency = (request.Currency ?? string.Empty).Trim().ToUpperInvariant();
        var reference = (request.Reference ?? string.Empty).Trim();
        if (request.Amount <= 0 || request.Amount > MaxAmount || decimal.Round(request.Amount, 2) != request.Amount
            || !CurrencyPattern().IsMatch(currency) || reference.Length is 0 or > 100)
            return Results.BadRequest(new ErrorResponse("invalid_request", "Amount, currency or reference is not valid."));

        // The same key again: the first charge is returned and nothing new is charged.
        var existing = await FindByKeyAsync(db, key, cancellationToken);
        if (existing is not null)
            return SameRequest(existing, request.Token, request.Amount, currency, reference)
                ? Results.Ok(ChargeResponse.From(existing))
                : Results.Conflict(new ErrorResponse("idempotency_conflict",
                    "This idempotency key was already used for a different charge."));

        var now = time.GetUtcNow().UtcDateTime;
        var card = await db.CardTokens.FirstOrDefaultAsync(c => c.Token == request.Token, cancellationToken);
        if (card is null)
        {
            // A sandbox test token (demo data) is registered on its first use.
            card = TestTokens.TryCreate(request.Token, now);
            if (card is null)
                return Results.BadRequest(new ErrorResponse(ChargeFailureCodes.InvalidCard, "Unknown card token."));
            db.CardTokens.Add(card);
        }

        var charge = new Charge
        {
            Id = Ids.New("ch"),
            IdempotencyKey = key,
            CardTokenId = card.Id,
            CardToken = card,
            Amount = request.Amount,
            Currency = currency,
            Reference = reference,
            Status = ChargeStatus.Pending,
            CreatedAt = now
        };

        db.Charges.Add(charge);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two requests with the same key at the same moment: the unique index let only one
            // through. The other one answers with the charge that was saved.
            db.ChangeTracker.Clear();
            var winner = await FindByKeyAsync(db, key, cancellationToken);
            if (winner is null)
                throw;
            return Results.Ok(ChargeResponse.From(winner));
        }

        logger.LogInformation("Charge {ChargeId} accepted: {Amount} {Currency} for {Reference}",
            charge.Id, charge.Amount, charge.Currency, charge.Reference);

        return Results.Accepted($"/v1/charges/{charge.Id}", ChargeResponse.From(charge));
    }

    private static async Task<IResult> GetChargeAsync(string id, SimulatorDbContext db, CancellationToken cancellationToken)
    {
        var charge = await db.Charges.AsNoTracking().Include(c => c.CardToken)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return charge is null
            ? Results.NotFound(new ErrorResponse("charge_not_found", "No charge with this id."))
            : Results.Ok(ChargeResponse.From(charge));
    }

    private static async Task<IResult> FindChargeAsync(string? idempotencyKey, SimulatorDbContext db, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Results.BadRequest(new ErrorResponse("invalid_request", "Give the idempotencyKey to search by."));

        var charge = await FindByKeyAsync(db, idempotencyKey.Trim(), cancellationToken);
        return charge is null
            ? Results.NotFound(new ErrorResponse("charge_not_found", "No charge with this idempotency key."))
            : Results.Ok(ChargeResponse.From(charge));
    }

    private static Task<Charge?> FindByKeyAsync(SimulatorDbContext db, string key, CancellationToken cancellationToken)
        => db.Charges.AsNoTracking().Include(c => c.CardToken)
            .FirstOrDefaultAsync(c => c.IdempotencyKey == key, cancellationToken);

    private static bool SameRequest(Charge charge, string? token, decimal amount, string currency, string reference)
        => charge.CardToken.Token == token && charge.Amount == amount
           && charge.Currency == currency && charge.Reference == reference;

    [GeneratedRegex("^[0-9]{3,4}$")]
    private static partial Regex CvvPattern();

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyPattern();
}
