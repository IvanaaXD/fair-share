using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class EmailRequestValidator : AbstractValidator<EmailRequest>
    {
        public EmailRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail address is required.")
                .EmailAddress().WithMessage("E-mail address is not valid.")
                .MaximumLength(256).WithMessage("E-mail address can have at most 256 characters.");
        }
    }
}