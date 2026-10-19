using FairShare.Application.DTOs.Settlements;
using FluentValidation;

namespace FairShare.Application.Validators;

public class SendReminderRequestValidator : AbstractValidator<SendReminderRequest>
{
    public const int MaxMessageLength = 300;

    public SendReminderRequestValidator()
    {
        RuleFor(x => x.Message)
            .MaximumLength(MaxMessageLength)
            .WithMessage($"Message can have at most {MaxMessageLength} characters.");
    }
}