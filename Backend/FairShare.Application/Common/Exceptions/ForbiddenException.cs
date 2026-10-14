namespace FairShare.Application.Common.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message, string? code = null) : base(message, code) { }
}
