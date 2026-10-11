using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Comments;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CommentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CommentResponse> CreateAsync(
        Guid groupId,
        Guid groupExpenseId,
        CreateCommentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var groupExpense = await ValidateGroupExpenseAndMembershipAsync(
            groupId, groupExpenseId, currentUserId, cancellationToken);

        var author = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var comment = _mapper.Map<Comment>(request);
        comment.CreatedAt = DateTime.UtcNow;
        comment.UserId = currentUserId;
        comment.User = author;
        comment.GroupExpenseId = groupExpense.Id;

        await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CommentResponse>(comment);
    }

    public async Task<IReadOnlyList<CommentResponse>> GetByGroupExpenseAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateGroupExpenseAndMembershipAsync(groupId, groupExpenseId, currentUserId, cancellationToken);

        var comments = await _unitOfWork.Comments.GetByGroupExpenseAsync(groupExpenseId, cancellationToken);
        return _mapper.Map<List<CommentResponse>>(comments);
    }

    public async Task DeleteAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, cancellationToken)
            ?? throw new NotFoundException("Comment not found.");

        if (comment.UserId != currentUserId)
            throw new ForbiddenException("You can only delete your own comment.");

        _unitOfWork.Comments.Remove(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ---------- helpers ----------

    /// <summary>The expense must exist, belong to the given group, and the user must be a member of that group.</summary>
    private async Task<GroupExpense> ValidateGroupExpenseAndMembershipAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var groupExpense = await _unitOfWork.GroupExpenses.GetByIdAsync(groupExpenseId, cancellationToken)
            ?? throw new NotFoundException("Group expense not found.");

        if (groupExpense.GroupId != groupId)
            throw new NotFoundException("The group expense does not belong to the specified group.");

        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken))
            throw new ForbiddenException("Only a group member can view or leave comments on group expenses.");

        return groupExpense;
    }
}