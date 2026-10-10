using FairShare.Application.DTOs.Files;

namespace FairShare.Application.Interfaces;

/// <summary>
/// Receipt photos of personal and group expenses. Kept apart from ExpenseService and
/// GroupExpenseService: creating an expense stays a plain JSON request, and the photo is
/// uploaded afterwards with a separate multipart request.
/// </summary>
public interface IReceiptService
{
    // ---------- personal expenses (owner only) ----------

    Task<ImageUrlResponse> UploadExpenseReceiptAsync(
        Guid expenseId, Guid currentUserId, ImageUpload upload, CancellationToken cancellationToken = default);

    Task<ImageFile> GetExpenseReceiptAsync(
        Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task DeleteExpenseReceiptAsync(
        Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default);

    // ---------- group expenses (members see, payer or owner change) ----------

    Task<ImageUrlResponse> UploadGroupExpenseReceiptAsync(
        Guid groupId, Guid groupExpenseId, Guid currentUserId, ImageUpload upload,
        CancellationToken cancellationToken = default);

    Task<ImageFile> GetGroupExpenseReceiptAsync(
        Guid groupId, Guid groupExpenseId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task DeleteGroupExpenseReceiptAsync(
        Guid groupId, Guid groupExpenseId, Guid currentUserId, CancellationToken cancellationToken = default);
}
