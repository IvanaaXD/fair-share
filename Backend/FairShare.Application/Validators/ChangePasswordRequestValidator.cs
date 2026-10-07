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

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Нова лозинка је обавезна.")
                .MinimumLength(8).WithMessage("Нова лозинка мора имати најмање 8 карактера.")
                .MaximumLength(128).WithMessage("Нова лозинка може имати највише 128 карактера.")
                .Matches("[A-Za-z]").WithMessage("Нова лозинка мора садржати бар једно слово.")
                .Matches("[0-9]").WithMessage("Нова лозинка мора садржати бар једну цифру.")
                .NotEqual(x => x.CurrentPassword).WithMessage("Нова лозинка мора бити различита од тренутне.");
        }
    }
}
