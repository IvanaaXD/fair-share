using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Qr;

public class ParsedQrResponse
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public string? RecipientAccount { get; set; }
    public string? RecipientName { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? PaymentCode { get; set; }
    public string? Purpose { get; set; }
    public string? Reference { get; set; }

    /// <summary>Поравнање из система на које се QR односи (ако га тренутни корисник смије видјети).</summary>
    public Guid? MatchedSettlementId { get; set; }
    public Guid? MatchedGroupId { get; set; }
    public SettlementStatus? MatchedSettlementStatus { get; set; }

    /// <summary>true ако QR одговара неизмиреном поравнању - frontend тада нуди дугме "Означи као измирено".</summary>
    public bool CanMarkAsSettled { get; set; }
}
