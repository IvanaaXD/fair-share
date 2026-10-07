using FairShare.Application.DTOs.Auth;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            // Username is the user's e-mail (IdentityService looks the user up by e-mail).
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("E-mail адреса је обавезна.")
                .EmailAddress().WithMessage("E-mail адреса није исправна.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Лозинка је обавезна.");
        }
    }
}
