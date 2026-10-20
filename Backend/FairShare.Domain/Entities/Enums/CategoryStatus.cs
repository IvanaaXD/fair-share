namespace FairShare.Domain.Entities.Enums;

/// <summary>
/// A category proposed by a user waits for an administrator. Approved is 0 on purpose: the
/// categories that existed before proposals were introduced get 0 in the migration and stay usable.
/// </summary>
public enum CategoryStatus
{
    Approved = 0,
    Pending = 1,
    Rejected = 2
}
