using FairShare.Application.DTOs.Categories;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
    {
        public CreateCategoryRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назив категорије је обавезан.")
                .MaximumLength(50).WithMessage("Назив категорије може имати највише 50 карактера.");

            RuleFor(x => x.Icon)
                .MaximumLength(50).WithMessage("Назив иконице може имати највише 50 карактера.");
        }
    }
}
