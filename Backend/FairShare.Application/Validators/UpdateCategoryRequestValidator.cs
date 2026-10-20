using FairShare.Application.DTOs.Categories;
using FluentValidation;

namespace FairShare.Application.Validators;

/// <summary>Renaming a category, and approving a proposal (the administrator may correct it).</summary>
public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).CategoryName();
        RuleFor(x => x.Icon).CategoryIcon();
    }
}
