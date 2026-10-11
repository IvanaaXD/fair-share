using FairShare.Application.DTOs.Groups;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
    {
        public UpdateGroupRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Group name is required.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Group name can have at most {ValidationRules.MaxNameLength} characters.");

            RuleFor(x => x.Description)
                .MaximumLength(ValidationRules.MaxDescriptionLength)
                .WithMessage($"Description can have at most {ValidationRules.MaxDescriptionLength} characters.");
        }
    }
}