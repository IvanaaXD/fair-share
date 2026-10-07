namespace FairShare.Application.Common.Exceptions
{
    /// <summary>
    /// Thrown when input is invalid in a way FluentValidation cannot check up front,
    /// e.g. query-string parameters that are not bound to a request DTO.
    /// Mapped to HTTP 400 by ExceptionHandlingMiddleware.
    /// </summary>
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
        }
    }
}
