namespace FairShare.Application.Common.Exceptions;

public class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException(string message, string? code = null) : base(message, code) { }
}
