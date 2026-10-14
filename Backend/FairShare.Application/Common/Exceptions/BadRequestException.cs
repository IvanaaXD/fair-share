namespace FairShare.Application.Common.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(string message, string? code = null) : base(message, code) { }
}
