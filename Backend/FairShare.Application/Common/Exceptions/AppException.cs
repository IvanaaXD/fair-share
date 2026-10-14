namespace FairShare.Application.Common.Exceptions;

/// <summary>
/// Base of the application's own exceptions. Besides the message (Serbian, for the user) it can
/// carry a stable, language-independent <see cref="Code"/> (see <see cref="ErrorCodes"/>). The API
/// returns it as "code" in the error response, and the web and mobile apps translate it into
/// the user's language; without a code they show the message itself.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, string? code) : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
