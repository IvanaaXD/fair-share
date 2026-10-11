using FairShare.Application.DTOs.Categories;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
    {
        public CreateCategoryRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(50).WithMessage("Category name can have at most 50 characters.");

            RuleFor(x => x.Icon)
                .MaximumLength(50).WithMessage("Icon name can have at most 50 characters.");
        }
    }
}