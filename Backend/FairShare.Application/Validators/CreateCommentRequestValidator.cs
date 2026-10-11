using FairShare.Application.DTOs.Comments;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
    {
        public CreateCommentRequestValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Comment text cannot be empty.")
                .MaximumLength(1000).WithMessage("Comment can have at most 1000 characters.");
        }
    }
}