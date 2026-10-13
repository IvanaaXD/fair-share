using FairShare.Application.DTOs.Payments;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class PayWithCardRequestValidator : AbstractValidator<PayWithCardRequest>
    {
        public PayWithCardRequestValidator()
        {
            RuleFor(x => x.PaymentCardId)
                .NotEmpty().WithMessage("Картица мора бити изабрана.");

            RuleFor(x => x.IdempotencyKey)
                .NotEmpty().WithMessage("Недостаје кључ захтјева (IdempotencyKey).")
                .MaximumLength(100).WithMessage("Кључ захтјева може имати највише 100 карактера.");
        }
    }
}
