using System.Text.RegularExpressions;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.ExchangeRates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

/// <summary>
/// Today's exchange rate between two currencies. The web app shows it while a group expense is
/// entered in a currency other than the group's ("≈ 117,17 KM · 1 EUR = 1,95583 KM"); the
/// server converts with the same rates when the expense is saved.
/// </summary>
[ApiController]
[Route("api/exchange-rates")]
[Authorize]
public partial class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateProvider _exchangeRates;

    public ExchangeRateController(IExchangeRateProvider exchangeRates)
    {
        _exchangeRates = exchangeRates;
    }

    /// <summary>GET api/exchange-rates?from=EUR&amp;to=BAM</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExchangeRateResponse>> Get(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken)
    {
        if (!IsCurrencyCode(from) || !IsCurrencyCode(to))
            throw new BadRequestException("Валута мора бити ознака од три слова, нпр. EUR.");

        var fromCode = from!.ToUpperInvariant();
        var toCode = to!.ToUpperInvariant();

        var rates = await _exchangeRates.GetRatesAsync(cancellationToken);
        if (!rates.TryGetRate(fromCode, toCode, out var rate))
            throw new ConflictException(
                $"Курс између {fromCode} и {toCode} тренутно није доступан.",
                ErrorCodes.ExchangeRateUnavailable);

        // The fixed EUR/BAM rate has no publication date; it is valid by law.
        return Ok(new ExchangeRateResponse(fromCode, toCode, rate, rates.UpdatedAt, rates.Source));
    }

    private static bool IsCurrencyCode(string? value) => value is not null && CurrencyCode().IsMatch(value);

    [GeneratedRegex("^[A-Za-z]{3}$")]
    private static partial Regex CurrencyCode();
}
