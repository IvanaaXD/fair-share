using FairShare.Application.DTOs.Categories;
using FluentValidation;

namespace FairShare.Application.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).CategoryName();
        RuleFor(x => x.Icon).CategoryIcon();
    }
}
