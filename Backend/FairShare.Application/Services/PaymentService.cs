using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Notifications;
using FairShare.Application.DTOs.Payments;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

/// <summary>
/// Paying a settlement with a saved card through the payment simulator service.
///
/// The payment is asynchronous, like with a real provider:
///   1. the same idempotency key again -> the earlier payment is returned, nothing is charged;
///   2. a Payment row with status Pending is saved BEFORE the provider is called;
///   3. the provider accepts the charge (or refuses it at once) - the user sees "processing";
///   4. a few seconds later the provider reports the result with a signed webhook, and the
///      payment and the settlement are completed together, in one transaction;
///   5. if no webhook arrives (e.g. the API was down), the status check asks the provider.
/// Steps 4 and 5 can never complete a payment twice: processed events are remembered, and
/// the payment and the settlement carry a row version (xmin).
/// </summary>
public class PaymentService : IPaymentService
{
    /// <summary>A pending payment without a webhook after this long is checked with the provider directly.</summary>
    private static readonly TimeSpan ReconcileAfter = TimeSpan.FromSeconds(10);

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

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<PaymentResponse> PayWithCardAsync(
        Guid groupId,
        Guid settlementTransactionId,
        PayWithCardRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1) A repeated request (e.g. after a timeout) gets the first payment, without a new charge.
        var repeated = await _unitOfWork.Payments.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (repeated is not null)
        {
            if (repeated.SettlementTransactionId != settlementTransactionId || repeated.PaymentCard.UserId != currentUserId)
                throw new ConflictException("Кључ захтјева је већ искоришћен за друго плаћање.", ErrorCodes.PaymentKeyReused);
            return await ToResponseAsync(repeated, cancellationToken);
        }

        var settlement = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(settlementTransactionId, cancellationToken);
        if (settlement is null || settlement.GroupId != groupId)
            throw new NotFoundException("Поравнање није пронађено.", ErrorCodes.SettlementNotFound);

        if (settlement.DebtorUserId != currentUserId)
            throw new ForbiddenException("Картицом може платити само члан који дугује.", ErrorCodes.PaymentOnlyDebtor);

        if (settlement.Status == SettlementStatus.Settled)
            throw new ConflictException("Поравнање је већ измирено.", ErrorCodes.SettlementAlreadySettled);

        var card = await _unitOfWork.PaymentCards.GetByIdAsync(request.PaymentCardId, cancellationToken);
        if (card is null || card.UserId != currentUserId)
            throw new NotFoundException("Картица није пронађена.", ErrorCodes.CardNotFound);

        if (card.ExpiryDate.Date < Now.Date)
            throw new ConflictException("Картица је истекла. Изаберите другу картицу.", ErrorCodes.CardExpired);

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.", ErrorCodes.GroupNotFound);

        // A settlement has at most one payment row (unique index). A failed attempt is reused.
        var payment = await _unitOfWork.Payments.GetBySettlementAsync(settlementTransactionId, cancellationToken);
        if (payment is { Status: PaymentStatus.Succeeded })
            throw new ConflictException("Поравнање је већ плаћено картицом.", ErrorCodes.PaymentAlreadyPaid);
        if (payment is { Status: PaymentStatus.Pending })
            throw new ConflictException("Плаћање овог поравнања је већ у току. Сачекајте неколико секунди.", ErrorCodes.PaymentInProgress);

        // 2) Pending record first: if the server stops during the call, the attempt is still visible.
        if (payment is null)
        {
            payment = new Payment
            {
                SettlementTransactionId = settlementTransactionId,
                CreatedAt = Now
            };
            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
        }
        payment.PaymentCardId = card.Id;
        payment.PaymentCard = card;
        payment.IdempotencyKey = request.IdempotencyKey;
        payment.Status = PaymentStatus.Pending;
        payment.FailureReason = null;
        payment.ProviderChargeId = null;
        payment.SubmittedAt = Now;
        payment.CompletedAt = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3) The provider accepts the charge; the result comes with the webhook.
        var charge = await _gateway.ChargeAsync(
            new GatewayChargeRequest(card.Token, settlement.Amount, group.Currency, request.IdempotencyKey, payment.Id.ToString()),
            cancellationToken);

        if (charge.Accepted)
        {
            payment.ProviderChargeId = charge.ChargeId;
        }
        else
        {
            // Refused at once (unknown card token) or the provider could not be reached. With
            // "gateway_unavailable" a late webhook may still complete the payment (see CanComplete).
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = charge.ErrorCode ?? PaymentErrorCodes.ProcessingError;
            payment.CompletedAt = Now;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(payment, settlement.Amount, group.Currency);
    }

    public async Task<PaymentResponse> GetPaymentStatusAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var settlement = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(settlementTransactionId, cancellationToken);
        if (settlement is null || settlement.GroupId != groupId)
            throw new NotFoundException("Поравнање није пронађено.", ErrorCodes.SettlementNotFound);

        if (settlement.DebtorUserId != currentUserId && settlement.CreditorUserId != currentUserId)
            throw new ForbiddenException("Плаћање може видјети само члан који дугује или члан коме се дугује.", ErrorCodes.SettlementNotParticipant);

        var payment = await _unitOfWork.Payments.GetBySettlementAsync(settlementTransactionId, cancellationToken)
            ?? throw new NotFoundException("Плаћање картицом за ово поравнање не постоји.", ErrorCodes.PaymentNotFound);

        // 5) No webhook for a while (the API was down, a wrong webhook address...): ask the provider.
        if (payment.Status == PaymentStatus.Pending && Now - payment.SubmittedAt >= ReconcileAfter)
            await ReconcileAsync(payment, settlement, cancellationToken);

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken);
        return Map(payment, settlement.Amount, group?.Currency ?? string.Empty);
    }

    public async Task<WebhookOutcome> HandleWebhookAsync(PaymentWebhookRequest webhook, CancellationToken cancellationToken = default)
    {
        // The simulator repeats a webhook until it gets 2xx - an event seen before changes nothing.
        if (await _unitOfWork.ProcessedWebhookEvents.ExistsAsync(webhook.Id, cancellationToken))
            return WebhookOutcome.Duplicate;

        var record = new ProcessedWebhookEvent
        {
            EventId = webhook.Id,
            Type = webhook.Type,
            ChargeId = webhook.Data.Id,
            CreatedAt = Now
        };
        await _unitOfWork.ProcessedWebhookEvents.AddAsync(record, cancellationToken);

        var outcome = webhook.Data.Status switch
        {
            "succeeded" => ChargeOutcome.Succeeded,
            "failed" => ChargeOutcome.Failed,
            _ => ChargeOutcome.Pending
        };

        var payment = Guid.TryParse(webhook.Data.Reference, out var paymentId)
            ? await _unitOfWork.Payments.GetWithDetailsAsync(paymentId, cancellationToken)
            : null;

        // The event must belong to the CURRENT attempt of the payment: a late webhook of an
        // earlier, failed attempt must not change the new one. If the charge id never reached
        // us (network error), the idempotency key identifies the attempt.
        var belongsToAttempt = payment is not null &&
            (payment.ProviderChargeId == webhook.Data.Id ||
             (payment.ProviderChargeId is null && payment.IdempotencyKey == webhook.Data.IdempotencyKey));

        if (payment is null || !belongsToAttempt || outcome == ChargeOutcome.Pending)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return WebhookOutcome.Ignored;
        }

        record.PaymentId = payment.Id;
        payment.ProviderChargeId ??= webhook.Data.Id;

        await CompleteAsync(payment, payment.SettlementTransaction, outcome, webhook.Data.FailureCode, cancellationToken);
        return WebhookOutcome.Processed;
    }

    // ---------- completing a payment ----------

    private async Task ReconcileAsync(Payment payment, SettlementTransaction settlement, CancellationToken cancellationToken)
    {
        ChargeStatusResult? status;
        try
        {
            status = payment.ProviderChargeId is not null
                ? await _gateway.GetChargeAsync(payment.ProviderChargeId, cancellationToken)
                : await _gateway.FindChargeAsync(payment.IdempotencyKey, cancellationToken);
        }
        catch (ServiceUnavailableException)
        {
            return; // the provider is down too - the payment simply stays pending for now
        }

        if (status is null)
        {
            // The provider has never heard of this charge: nothing was charged.
            await CompleteAsync(payment, settlement, ChargeOutcome.Failed, PaymentErrorCodes.ProcessingError, cancellationToken);
            return;
        }

        if (status.Outcome == ChargeOutcome.Pending)
            return;

        payment.ProviderChargeId ??= status.ChargeId;
        await CompleteAsync(payment, settlement, status.Outcome, status.FailureCode, cancellationToken);
    }

    /// <summary>
    /// Pending -> Succeeded (and the settlement becomes settled) or Failed, saved in one
    /// transaction. One exception: a payment marked "gateway_unavailable" may still turn into
    /// Succeeded, because the provider may have charged the card even though its answer was lost.
    /// </summary>
    private async Task CompleteAsync(
        Payment payment,
        SettlementTransaction settlement,
        ChargeOutcome outcome,
        string? failureCode,
        CancellationToken cancellationToken)
    {
        var canComplete = payment.Status == PaymentStatus.Pending ||
            (outcome == ChargeOutcome.Succeeded && payment.Status == PaymentStatus.Failed &&
             payment.FailureReason == PaymentErrorCodes.GatewayUnavailable);

        if (!canComplete)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken); // e.g. the webhook record
            return;
        }

        var succeeded = outcome == ChargeOutcome.Succeeded;
        var settlementWasOpen = settlement.Status != SettlementStatus.Settled;

        payment.Status = succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed;
        payment.FailureReason = succeeded ? null : failureCode ?? PaymentErrorCodes.ProcessingError;
        payment.CompletedAt = Now;

        if (succeeded)
            settlement.Status = SettlementStatus.Settled;

        // If the webhook and the status check complete the payment at the same moment, the row
        // version (xmin) lets only one save through; the other one gets HTTP 409 and the
        // simulator simply tries again later, finding the payment already completed.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyAsync(payment, settlement, succeeded, settlementWasOpen, cancellationToken);
    }

    private async Task NotifyAsync(
        Payment payment,
        SettlementTransaction settlement,
        bool succeeded,
        bool settlementWasOpen,
        CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(settlement.GroupId, cancellationToken);
        var amount = NotificationText.Money(settlement.Amount, group?.Currency);
        var groupName = NotificationText.Quote(group?.Name ?? string.Empty);
        var card = $"{payment.PaymentCard.Brand} •••• {payment.PaymentCard.Last4Digits}";

        if (!succeeded)
        {
            // Only the payer needs to know; the app shows the reason (in-app, no e-mail).
            await _notificationService.NotifyAsync(
                settlement.DebtorUserId,
                NotificationType.PaymentFailed,
                "Плаћање није успјело",
                $"Плаћање картицом {card} није успјело ({amount} у групи {groupName}). Покушај поново или изабери другу картицу.",
                cancellationToken,
                sendEmail: false);
            return;
        }

        if (settlementWasOpen)
        {
            // The member who receives the money.
            await _notificationService.NotifyAsync(
                settlement.CreditorUserId,
                NotificationType.PaymentConfirmation,
                "Стигла је уплата",
                $"Стигла је уплата картицом у групи {groupName}: {NotificationText.FullName(settlement.DebtorUser)}, {amount}. Поравнање је измирено.",
                cancellationToken);
        }

        // The member who paid gets a confirmation (a receipt in the app and by e-mail).
        await _notificationService.NotifyAsync(
            settlement.DebtorUserId,
            NotificationType.PaymentConfirmation,
            "Потврда плаћања",
            $"Плаћање картицом {card} је успјело: {amount} за {NotificationText.FullName(settlement.CreditorUser)} у групи {groupName}.",
            cancellationToken);
    }

    // ---------- helpers ----------

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
