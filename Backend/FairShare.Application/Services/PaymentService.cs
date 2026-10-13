using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Payments;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

/// <summary>
/// Paying a settlement with a saved card through the payment simulator.
///
/// Order of steps, chosen so that money is never charged twice and never charged without
/// a record of it:
///   1. the same idempotency key again -> the earlier result is returned, nothing is charged;
///   2. a Payment row with status Pending is saved BEFORE the card is charged;
///   3. the simulator charges the card (with the same idempotency key);
///   4. the result is saved together with the settlement status, in one transaction.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _gateway;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IPaymentGateway gateway,
        INotificationService notificationService,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _gateway = gateway;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    public async Task<PaymentResponse> PayWithCardAsync(
        Guid groupId,
        Guid settlementTransactionId,
        PayWithCardRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1) A repeated request (e.g. after a timeout) gets the first result, without a new charge.
        var repeated = await _unitOfWork.Payments.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (repeated is not null)
        {
            if (repeated.SettlementTransactionId != settlementTransactionId || repeated.PaymentCard.UserId != currentUserId)
                throw new ConflictException("Кључ захтјева је већ искоришћен за друго плаћање.");
            return await ToResponseAsync(repeated, cancellationToken);
        }

        var settlement = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(settlementTransactionId, cancellationToken);
        if (settlement is null || settlement.GroupId != groupId)
            throw new NotFoundException("Поравнање није пронађено.");

        if (settlement.DebtorUserId != currentUserId)
            throw new ForbiddenException("Картицом може платити само члан који дугује.");

        if (settlement.Status == SettlementStatus.Settled)
            throw new ConflictException("Поравнање је већ измирено.");

        var card = await _unitOfWork.PaymentCards.GetByIdAsync(request.PaymentCardId, cancellationToken);
        if (card is null || card.UserId != currentUserId)
            throw new NotFoundException("Картица није пронађена.");

        if (card.ExpiryDate.Date < _timeProvider.GetUtcNow().UtcDateTime.Date)
            throw new ConflictException("Картица је истекла. Изаберите другу картицу.");

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.");

        // A settlement has at most one payment row (unique index). A failed attempt is reused.
        var payment = await _unitOfWork.Payments.GetBySettlementAsync(settlementTransactionId, cancellationToken);
        if (payment is { Status: PaymentStatus.Succeeded })
            throw new ConflictException("Поравнање је већ плаћено картицом.");
        if (payment is { Status: PaymentStatus.Pending })
            throw new ConflictException("Плаћање овог поравнања је већ у току. Сачекајте неколико секунди.");

        // 2) Pending record first: if the server stops during the charge, the attempt is still visible.
        if (payment is null)
        {
            payment = new Payment
            {
                SettlementTransactionId = settlementTransactionId,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
        }
        payment.PaymentCardId = card.Id;
        payment.PaymentCard = card;
        payment.IdempotencyKey = request.IdempotencyKey;
        payment.Status = PaymentStatus.Pending;
        payment.FailureReason = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3) The charge itself.
        var charge = await _gateway.ChargeAsync(
            card.Token, settlement.Amount, group.Currency, request.IdempotencyKey, cancellationToken);

        // 4) The result and the settlement change together: either both are saved or neither.
        if (charge.Success)
        {
            payment.Status = PaymentStatus.Succeeded;
            // If the settlement was marked as settled in the meantime (QR, cash), its row version
            // has changed and this save fails with HTTP 409 instead of settling it twice.
            settlement.Status = SettlementStatus.Settled;
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = charge.ErrorCode ?? PaymentErrorCodes.ProcessingError;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (charge.Success)
        {
            var debtor = $"{settlement.DebtorUser.FirstName} {settlement.DebtorUser.LastName}";
            await _notificationService.NotifyAsync(
                settlement.CreditorUserId,
                NotificationType.PaymentConfirmation,
                "Уплата картицом",
                $"{debtor}: уплата картицом од {settlement.Amount:0.00} {group.Currency} у групи '{group.Name}'. Поравнање је измирено.",
                cancellationToken);
        }

        return Map(payment, settlement.Amount, group.Currency);
    }

    private async Task<PaymentResponse> ToResponseAsync(Payment payment, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(payment.SettlementTransaction.GroupId, cancellationToken);
        return Map(payment, payment.SettlementTransaction.Amount, group?.Currency ?? string.Empty);
    }

    private static PaymentResponse Map(Payment payment, decimal amount, string currency) => new()
    {
        Id = payment.Id,
        SettlementTransactionId = payment.SettlementTransactionId,
        Status = payment.Status,
        FailureReason = payment.FailureReason,
        Amount = amount,
        Currency = currency,
        CardBrand = payment.PaymentCard.Brand,
        CardLast4Digits = payment.PaymentCard.Last4Digits,
        CreatedAt = payment.CreatedAt
    };
}
