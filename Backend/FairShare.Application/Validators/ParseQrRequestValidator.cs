using FairShare.Application.DTOs.Qr;
using FluentValidation;

namespace FairShare.Application.Validators
{
    /// <summary>
    /// Only checks that something was scanned. The content itself is validated by IpsQrCodec,
    /// which returns a detailed list of problems instead of a plain 400.
    /// </summary>
    public class ParseQrRequestValidator : AbstractValidator<ParseQrRequest>
    {
        public ParseQrRequestValidator()
        {
            RuleFor(x => x.Payload)
                .NotEmpty().WithMessage("The QR code content is empty.")
                .MaximumLength(1000).WithMessage("The QR code content is too long.");
        }
    }
}