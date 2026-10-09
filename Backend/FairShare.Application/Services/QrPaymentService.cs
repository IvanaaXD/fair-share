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
    /// <summary>Шифра плаћања 289 - трансакције по налогу грађана.</summary>
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
        var purpose = $"Поравнање FairShare - {group.Name}";

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

        // Позив на број садржи ID поравнања - ако га препознамо, повезујемо QR са системом.
        if (p.Reference is not null && Guid.TryParseExact(p.Reference, "N", out var settlementId))
        {
            var settlement = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(settlementId, cancellationToken);

            // Туђа поравнања се не откривају - корисник види само она у којима учествује.
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

    // ---------- помоћне методе ----------

    private async Task<(SettlementTransaction Settlement, Group Group)> LoadForQrAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var settlement = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(
            settlementTransactionId, cancellationToken)
            ?? throw new NotFoundException("Трансакција поравнања није пронађена.");

        if (settlement.GroupId != groupId)
            throw new NotFoundException("Трансакција поравнања не припада наведеној групи.");

        if (settlement.DebtorUserId != currentUserId && settlement.CreditorUserId != currentUserId)
            throw new ForbiddenException("QR код могу видјети само дужник и повјерилац.");

        if (settlement.Status == SettlementStatus.Settled)
            throw new ConflictException("Поравнање је већ измирено - QR код више није потребан.");

        if (!IpsQrCodec.IsValidAccount(settlement.CreditorUser.BankAccountNumber))
            throw new ConflictException(
                "Повјерилац још није унио број рачуна у профил, па QR код за уплату није могуће направити.");

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.");

        // Рачун је можда унесен тек након генерисања приједлога - освјежавамо сачуване QR податке.
        var hadQrData = settlement.QrPaymentData is not null;
        var accountBefore = settlement.QrPaymentData?.RecipientAccount;

        settlement.Group = group;
        var qrData = settlement.GenerateQrCode();

        if (!hadQrData)
        {
            // Нови запис се додаје експлицитно: Id је већ постављен (BaseEntity), па би га
            // EF иначе третирао као постојећи ред и покушао UPDATE умјесто INSERT.
            await _unitOfWork.QrPaymentData.AddAsync(qrData, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (accountBefore != qrData.RecipientAccount)
        {
            // Постојећи запис је праћен (tracked), па EF сам открива промјену.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return (settlement, group);
    }
}
