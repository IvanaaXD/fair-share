namespace FairShare.Domain.Entities.Enums;

/// <summary>
/// State of a card payment of a settlement.
/// Pending   - the payment simulator has been called and has not answered yet;
/// Succeeded - the money was charged, the settlement is settled;
/// Failed    - the card was declined; the debtor can try again (with the same or another card).
/// </summary>
public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed
}
