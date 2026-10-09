using FairShare.Application.DTOs.Users;
using FluentValidation;

namespace FairShare.Application.Validators
{
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Тренутна лозинка је обавезна.");

            // CHANGED: uses the shared password policy, so registration, reset and change agree.
            RuleFor(x => x.NewPassword)
                .ValidNewPassword()
                .NotEqual(x => x.CurrentPassword).WithMessage("Нова лозинка мора бити различита од тренутне.");
        }
    }
}
