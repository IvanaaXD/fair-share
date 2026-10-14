namespace FairShare.Application.Common.Exceptions;

public class ValidationException : AppException
{
    public ValidationException(string message, string? code = null) : base(message, code) { }
}
