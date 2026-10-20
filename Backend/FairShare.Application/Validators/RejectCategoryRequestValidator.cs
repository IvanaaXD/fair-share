using FairShare.Application.DTOs.Categories;
using FairShare.Domain.Entities;
using FluentValidation;

namespace FairShare.Application.Validators;

public class RejectCategoryRequestValidator : AbstractValidator<RejectCategoryRequest>
{
    public RejectCategoryRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithMessage("Наведи разлог одбијања.")
            .MaximumLength(Category.MaxRejectionReasonLength)
            .WithMessage($"Разлог може имати највише {Category.MaxRejectionReasonLength} карактера.");
    }
}
