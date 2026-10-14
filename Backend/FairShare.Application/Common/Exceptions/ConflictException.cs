namespace FairShare.Application.Common.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message, string? code = null) : base(message, code) { }
}
