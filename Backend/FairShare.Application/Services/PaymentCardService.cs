using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Payments;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;

namespace FairShare.Application.Services;

/// <summary>
/// The user's saved payment cards. Card numbers and CVV codes pass through this service on the
/// way to the payment simulator, but are never stored or logged - only the token is.
/// </summary>
public class PaymentCardService : IPaymentCardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _gateway;
    private readonly TimeProvider _timeProvider;

    public PaymentCardService(IUnitOfWork unitOfWork, IPaymentGateway gateway, TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _gateway = gateway;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<PaymentCardResponse>> GetMyCardsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var cards = await _unitOfWork.PaymentCards.GetByUserAsync(currentUserId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return cards.Select(card => ToResponse(card, now)).ToList();
    }

    public async Task<PaymentCardResponse> AddCardAsync(
        Guid currentUserId,
        AddPaymentCardRequest request,
        CancellationToken cancellationToken = default)
    {
        // Formats (Luhn check, expiry, CVV) are checked by AddPaymentCardRequestValidator.
        var result = await _gateway.TokenizeCardAsync(
            new CardDetails(
                CardNumber.Normalize(request.CardNumber),
                request.ExpiryMonth,
                request.ExpiryYear,
                request.Cvv,
                request.HolderName?.Trim()),
            cancellationToken);

        if (!result.Success || result.Token is null)
            throw new BadRequestException("Card not accepted. Please check the card number, expiration date, and CVV.");

        var card = new PaymentCard
        {
            UserId = currentUserId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Token = result.Token,
            Brand = result.Brand ?? CardNumber.DetectBrand(request.CardNumber),
            Last4Digits = result.Last4Digits ?? CardNumber.Normalize(request.CardNumber)[^4..],
            // Stored as the last day of the expiry month; only month and year are shown.
            ExpiryDate = new DateTime(request.ExpiryYear, request.ExpiryMonth, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(1).AddDays(-1)
        };

        await _unitOfWork.PaymentCards.AddAsync(card, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(card, _timeProvider.GetUtcNow().UtcDateTime);
    }

    public async Task DeleteCardAsync(Guid currentUserId, Guid paymentCardId, CancellationToken cancellationToken = default)
    {
        var card = await _unitOfWork.PaymentCards.GetByIdAsync(paymentCardId, cancellationToken);

        // Someone else's card is reported exactly like a missing one.
        if (card is null || card.UserId != currentUserId)
            throw new NotFoundException("Card not found.");

        // Payments keep a reference to the card they were made with (ON DELETE RESTRICT).
        if (await _unitOfWork.PaymentCards.HasPaymentsAsync(paymentCardId, cancellationToken))
            throw new ConflictException("The card has been used for payments and cannot be deleted.");

        _unitOfWork.PaymentCards.Remove(card);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public static PaymentCardResponse ToResponse(PaymentCard card, DateTime now) => new()
    {
        Id = card.Id,
        Brand = card.Brand,
        Last4Digits = card.Last4Digits,
        ExpiryMonth = card.ExpiryDate.Month,
        ExpiryYear = card.ExpiryDate.Year,
        IsExpired = card.ExpiryDate.Date < now.Date
    };
}