using FairShare.Application.DTOs.Qr;

namespace FairShare.Application.Interfaces;

public interface IQrPaymentService
{
    /// <summary>Текст QR кода за неизмирено поравнање (само дужник или повјерилац).</summary>
    Task<QrPayloadResponse> GetPayloadAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>PNG слика истог QR кода.</summary>
    Task<byte[]> GetImageAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Валидира скенирани текст и проналази поравнање на које се односи.</summary>
    Task<ParsedQrResponse> ParseAsync(
        ParseQrRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
