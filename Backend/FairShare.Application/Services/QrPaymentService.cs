using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Qr;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;

namespace FairShare.Application.Services;

public class QrPaymentService : IQrPaymentService
{
    /// <summary>Payment code 289 - transactions ordered by citizens.</summary>
    private const string PaymentCode = "289";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IQrCodeImageGenerator _imageGenerator;

    public QrPaymentService(IUnitOfWork unitOfWork, IQrCodeImageGenerator imageGenerator)
    {
        _unitOfWork = unitOfWork;
        _imageGenerator = imageGenerator;
    }

    public async Task<QrPayloadResponse> GetPayloadAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (settlement, group) = await LoadForQrAsync(groupId, settlementTransactionId, currentUserId, cancellationToken);

        var qrData = settlement.QrPaymentData!;
        var recipientName = $"{settlement.CreditorUser.FirstName} {settlement.CreditorUser.LastName}";
        var purpose = $"FairShare settlement - {group.Name}";

        var payload = IpsQrCodec.Build(new IpsQrPayload(
            qrData.RecipientAccount,
            recipientName,
            qrData.Currency,
            qrData.Amount,
            PaymentCode,
            purpose,
            qrData.ReferenceCode));

        return new QrPayloadResponse
        {
            SettlementTransactionId = settlement.Id,
            Payload = payload,
            RecipientAccount = qrData.RecipientAccount,
            RecipientName = recipientName,
            Amount = qrData.Amount,
            Currency = qrData.Currency,
            Purpose = purpose.Length <= IpsQrCodec.MaxPurposeLength ? purpose : purpose[..IpsQrCodec.MaxPurposeLength],
            ReferenceCode = qrData.ReferenceCode
        };
    }

    public async Task<byte[]> GetImageAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var payload = await GetPayloadAsync(groupId, settlementTransactionId, currentUserId, cancellationToken);
        return _imageGenerator.GeneratePng(payload.Payload);
    }

    public async Task<ParsedQrResponse> ParseAsync(
        ParseQrRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var result = IpsQrCodec.Parse(request.Payload);
        if (!result.IsValid || result.Payload is null)
            return new ParsedQrResponse { IsValid = false, Errors = result.Errors.ToList() };

        var p = result.Payload;
        var response = new ParsedQrResponse
        {
            IsValid = true,
            RecipientAccount = p.RecipientAccount,
            RecipientName = p.RecipientName,
            Amount = p.Amount,
            Currency = p.Currency,
            PaymentCode = p.PaymentCode,
            Purpose = p.Purpose,
            Reference = p.Reference
        };

        // The reference number contains the settlement ID - if recognized, we link the QR to the system.
        if (p.Reference is not null && Guid.TryParseExact(p.Reference, "N", out var settlementId))
        {
            var settlement = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(settlementId, cancellationToken);

            // Other people's settlements are not revealed - the user only sees those they participate in.
            if (settlement is not null &&
                (settlement.DebtorUserId == currentUserId || settlement.CreditorUserId == currentUserId))
            {
                response.MatchedSettlementId = settlement.Id;
                response.MatchedGroupId = settlement.GroupId;
                response.MatchedSettlementStatus = settlement.Status;
                response.CanMarkAsSettled = settlement.Status == SettlementStatus.Proposed
                                            && settlement.Amount == p.Amount;
            }
        }

        return response;
    }

    // ---------- helpers ----------

    private async Task<(SettlementTransaction Settlement, Group Group)> LoadForQrAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var settlement = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(
            settlementTransactionId, cancellationToken)
            ?? throw new NotFoundException("Settlement transaction not found.");

        if (settlement.GroupId != groupId)
            throw new NotFoundException("The settlement transaction does not belong to the specified group.");

        if (settlement.DebtorUserId != currentUserId && settlement.CreditorUserId != currentUserId)
            throw new ForbiddenException("The QR code can only be viewed by the debtor and the creditor.");

        if (settlement.Status == SettlementStatus.Settled)
            throw new ConflictException("The settlement is already settled - the QR code is no longer needed.");

        if (!IpsQrCodec.IsValidAccount(settlement.CreditorUser.BankAccountNumber))
            throw new ConflictException(
                "The creditor has not yet entered a bank account number in their profile, so the payment QR code cannot be generated.");

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        // The account might have been entered only after the proposal was generated - we refresh the saved QR data.
        var hadQrData = settlement.QrPaymentData is not null;
        var accountBefore = settlement.QrPaymentData?.RecipientAccount;

        settlement.Group = group;
        var qrData = settlement.GenerateQrCode();

        if (!hadQrData)
        {
            // A new record is added explicitly: the Id is already set (BaseEntity), so
            // EF would otherwise treat it as an existing row and attempt an UPDATE instead of INSERT.
            await _unitOfWork.QrPaymentData.AddAsync(qrData, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (accountBefore != qrData.RecipientAccount)
        {
            // The existing record is tracked, so EF detects the change itself.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return (settlement, group);
    }
}