using FairShare.Application.DTOs.Payments;
using FluentValidation;

namespace FairShare.Application.Validators;

public class PayWithCardRequestValidator : AbstractValidator<PayWithCardRequest>
{
    public PayWithCardRequestValidator()
    {
        RuleFor(x => x.PaymentCardId)
            .NotEmpty().WithMessage("A payment card must be selected.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Request key (IdempotencyKey) is missing.")
            .MaximumLength(100).WithMessage("Request key can have at most 100 characters.");
    }
}