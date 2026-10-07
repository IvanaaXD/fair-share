namespace FairShare.Domain.Exceptions
{
    /// <summary>
    /// Thrown when a domain rule is violated (e.g. split amounts do not add up to the total).
    /// Lives in the Domain layer so domain services do not depend on Application exceptions.
    /// Mapped to HTTP 400 by ExceptionHandlingMiddleware.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }
    }
}
