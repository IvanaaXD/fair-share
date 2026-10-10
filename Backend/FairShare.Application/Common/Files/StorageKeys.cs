using System.Globalization;

namespace FairShare.Application.Common.Files;

/// <summary>
/// The single place that decides where each kind of file is stored. Keys are built only from
/// ids, never from anything the client sends (such as the original file name), so a client can
/// neither choose a path nor point an expense at somebody else's file.
///
/// Receipts are grouped by owner (user or group), so deleting a group or a user can remove all
/// of its files at once (see FileCleanupInterceptor).
/// </summary>
public static class StorageKeys
{
    public static string ProfileImage(Guid userId) => $"profile-images/{userId:N}";

    public static string UserReceiptsFolder(Guid userId) => $"receipts/users/{userId:N}";

    public static string ExpenseReceipt(Guid userId, Guid expenseId)
        => $"{UserReceiptsFolder(userId)}/{expenseId:N}";

    public static string GroupReceiptsFolder(Guid groupId) => $"receipts/groups/{groupId:N}";

    public static string GroupExpenseReceipt(Guid groupId, Guid groupExpenseId)
        => $"{GroupReceiptsFolder(groupId)}/{groupExpenseId:N}";
}

/// <summary>
/// API addresses from which the images are downloaded; these are what is stored in
/// ProfileImageUrl / ReceiptImageUrl and returned to the frontend.
///
/// The "v" parameter changes on every upload. The download endpoints ignore it, but the browser
/// treats a new value as a new address, so after replacing an image it never shows the old one
/// from its cache.
/// </summary>
public static class ImageUrls
{
    public static string ProfileImage(Guid userId)
        => $"/api/users/{userId}/profile-image?v={NewVersion()}";

    public static string ExpenseReceipt(Guid expenseId)
        => $"/api/expenses/{expenseId}/receipt?v={NewVersion()}";

    public static string GroupExpenseReceipt(Guid groupId, Guid groupExpenseId)
        => $"/api/groups/{groupId}/expenses/{groupExpenseId}/receipt?v={NewVersion()}";

    private static string NewVersion() => DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
}
