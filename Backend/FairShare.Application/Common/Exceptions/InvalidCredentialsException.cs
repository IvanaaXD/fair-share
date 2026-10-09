namespace FairShare.Application.Common.Exceptions
{
    /// <summary>
    /// Thrown when sign-in fails: wrong e-mail or password, or an invalid / expired refresh token.
    /// It derives from UnauthorizedAccessException, which ExceptionHandlingMiddleware already maps
    /// to HTTP 401, so no change in the middleware is required for the correct status code.
    /// </summary>
    public class InvalidCredentialsException : UnauthorizedAccessException
    {
        public InvalidCredentialsException(string message) : base(message)
        {
        }
    }
}
