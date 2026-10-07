using FairShare.Application.DTOs.Groups;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class AddGroupMemberRequestValidator : AbstractValidator<AddGroupMemberRequest>
    {
        public AddGroupMemberRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail адреса је обавезна.")
                .EmailAddress().WithMessage("E-mail адреса није исправна.")
                .MaximumLength(256).WithMessage("E-mail адреса може имати највише 256 карактера.");
        }
    }
}
