using FairShare.Application.DTOs.Users;
using FluentValidation;

namespace FairShare.Application.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        // Uses the shared password policy, so registration, reset and change agree.
        RuleFor(x => x.NewPassword)
            .ValidNewPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from the current password.");
    }
}