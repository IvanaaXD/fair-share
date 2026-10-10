using System.Security.Claims;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FairShare.Api.Filters;

/// <summary>
/// A global filter that automatically logs every SUCCESSFUL (2xx) POST/PUT/PATCH/DELETE action
/// of the logged-in user into the audit log (feature 5.11). Services do not need to be
/// changed - a new action in any controller is automatically tracked.
/// </summary>
public class AuditLogFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> AuditedMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogFilter> _logger;

    public AuditLogFilter(IAuditLogService auditLogService, ILogger<AuditLogFilter> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        var http = context.HttpContext;

        if (!AuditedMethods.Contains(http.Request.Method))
            return;

        if (executed.Exception is not null && !executed.ExceptionHandled)
            return;

        if (context.ActionDescriptor.EndpointMetadata.OfType<SkipAuditAttribute>().Any())
            return;

        if (!Guid.TryParse(http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return; // anonymous requests (e.g., login) are logged elsewhere

        var statusCode = executed.Result is IStatusCodeActionResult { StatusCode: { } code }
            ? code
            : http.Response.StatusCode;
        if (statusCode is < 200 or >= 300)
            return;

        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var controller = descriptor?.ControllerName ?? "Unknown";
        var action = $"{controller}.{descriptor?.ActionName ?? "Unknown"}";

        try
        {
            await _auditLogService.LogAsync(userId, action, controller, ResolveEntityId(context, executed), http.RequestAborted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed write to the log must not crash a request that has already succeeded.
            _logger.LogError(ex, "Writing to the audit log failed for action {Action}.", action);
        }
    }

    /// <summary>
    /// Entity ID: first from the response (e.g., a newly created expense has an Id), otherwise the last
    /// Guid parameter from the route (e.g., userId when blocking).
    /// </summary>
    private static Guid? ResolveEntityId(ActionExecutingContext context, ActionExecutedContext executed)
    {
        if (executed.Result is ObjectResult { Value: { } value } &&
            value.GetType().GetProperty("Id")?.GetValue(value) is Guid idFromResponse &&
            idFromResponse != Guid.Empty)
        {
            return idFromResponse;
        }

        var routeGuidParameter = context.ActionDescriptor.Parameters
            .LastOrDefault(p => p.ParameterType == typeof(Guid) && p.BindingInfo?.BindingSource == BindingSource.Path);

        if (routeGuidParameter is not null &&
            context.ActionArguments.TryGetValue(routeGuidParameter.Name, out var routeValue) &&
            routeValue is Guid idFromRoute)
        {
            return idFromRoute;
        }

        return null;
    }
}
