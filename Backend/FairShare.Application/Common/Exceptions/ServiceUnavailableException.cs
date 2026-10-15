namespace FairShare.Application.Common.Exceptions;

/// <summary>An outside service (e.g. the payment simulator) cannot be reached - HTTP 503.</summary>
public class ServiceUnavailableException : AppException
{
    public ServiceUnavailableException(string message, string? code = null) : base(message, code) { }
}
