namespace FairShare.Application.Common.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message, string? code = null) : base(message, code) { }
}
