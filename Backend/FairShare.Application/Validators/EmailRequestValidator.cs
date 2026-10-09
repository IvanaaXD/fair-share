using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class EmailRequestValidator : AbstractValidator<EmailRequest>
    {
        public EmailRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail адреса је обавезна.")
                .EmailAddress().WithMessage("E-mail адреса није исправна.")
                .MaximumLength(256).WithMessage("E-mail адреса може имати највише 256 карактера.");
        }
    }
}
