using FairShare.Application.DTOs.Comments;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
    {
        public CreateCommentRequestValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Текст коментара не смије бити празан.")
                .MaximumLength(1000).WithMessage("Коментар може имати највише 1000 карактера.");
        }
    }
}
