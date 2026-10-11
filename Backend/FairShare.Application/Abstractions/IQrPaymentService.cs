using FairShare.Application.DTOs.Qr;

namespace FairShare.Application.Interfaces;

public interface IQrPaymentService
{
    /// <summary>QR code text for an outstanding settlement (debtor or creditor only).</summary>
    Task<QrPayloadResponse> GetPayloadAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>PNG image of the same QR code.</summary>
    Task<byte[]> GetImageAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Validates the scanned text and finds the settlement it refers to.</summary>
    Task<ParsedQrResponse> ParseAsync(
        ParseQrRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
