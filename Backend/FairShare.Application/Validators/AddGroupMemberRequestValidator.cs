using FairShare.Application.DTOs.Groups;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class AddGroupMemberRequestValidator : AbstractValidator<AddGroupMemberRequest>
    {
        public AddGroupMemberRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail address is required.")
                .EmailAddress().WithMessage("E-mail address is not valid.")
                .MaximumLength(256).WithMessage("E-mail address can have at most 256 characters.");
        }
    }
}