using FairShare.Application.DTOs.Groups;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
    {
        public CreateGroupRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назив групе је обавезан.")
                .MaximumLength(ValidationRules.MaxNameLength)
                .WithMessage($"Назив групе може имати највише {ValidationRules.MaxNameLength} карактера.");

            RuleFor(x => x.Description)
                .MaximumLength(ValidationRules.MaxDescriptionLength)
                .WithMessage($"Опис може имати највише {ValidationRules.MaxDescriptionLength} карактера.");

            RuleFor(x => x.Currency).ValidCurrency();
        }
    }
}
