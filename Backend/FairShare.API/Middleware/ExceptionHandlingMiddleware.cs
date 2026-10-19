using FairShare.Application.Common.Exceptions;
using FairShare.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValidationException = FluentValidation.ValidationException;

namespace FairShare.API.Middleware
{
    /// <summary>
    /// Catches every unhandled exception and turns it into a ProblemDetails response with the
    /// right HTTP status code. Services can simply throw NotFoundException, ForbiddenException
    /// etc., and controllers stay free of try/catch blocks.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The client closed the connection - there is nobody to send a response to.
            }
            catch (Exception exception)
            {
                if (context.Response.HasStarted)
                    throw; // headers already sent, the response can no longer be changed

                await WriteProblemAsync(context, exception);
            }
        }

        private async Task WriteProblemAsync(HttpContext context, Exception exception)
        {
            var (status, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found."),
                ForbiddenException => (StatusCodes.Status403Forbidden, "You do not have permission for this action."),
                ConflictException => (StatusCodes.Status409Conflict, "The request conflicts with the current state."),
                BadRequestException => (StatusCodes.Status400BadRequest, "The request is invalid."),
                ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "The service is currently unavailable."),
                DomainException => (StatusCodes.Status400BadRequest, "A business rule was violated."),
                ValidationException => (StatusCodes.Status400BadRequest, "The data in the request is invalid."),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "You are not authenticated."),
                // Optimistic concurrency: someone else changed the same row in the meantime.
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The data has been modified in the meantime. Please refresh and try again."),
                // Usually a unique index violation (e.g. two requests creating the same category at once).
                DbUpdateException => (StatusCodes.Status409Conflict, "The data cannot be saved because it conflicts with existing data."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("{ExceptionType} for {Method} {Path}: {Message}",
                    exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Instance = context.Request.Path,
                // Messages of our own exceptions are meant for the user. For 500 errors and database
                // errors the technical message is shown only in Development, never in production.
                Detail = IsSafeToExpose(exception) || _environment.IsDevelopment()
                    ? exception.Message
                    : null
            };

            // A stable code the apps translate into the user's language (ErrorCodes).
            var code = exception switch
            {
                AppException { Code: { } own } => own,
                DbUpdateConcurrencyException => ErrorCodes.DataChanged,
                DbUpdateException => ErrorCodes.DataConflict,
                _ when status == StatusCodes.Status500InternalServerError => ErrorCodes.ServerError,
                _ => null
            };
            if (code is not null)
                problem.Extensions["code"] = code;

            if (exception is ValidationException validationException)
            {
                problem.Extensions["errors"] = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }

        private static bool IsSafeToExpose(Exception exception) => exception is
            NotFoundException or ForbiddenException or ConflictException or
            BadRequestException or ServiceUnavailableException or DomainException or ValidationException;
    }
}
