using System.Security.Claims;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace FairShare.Api.Filters;

/// <summary>
/// Глобални филтер који аутоматски биљежи сваку УСПЈЕШНУ (2xx) POST/PUT/PATCH/DELETE акцију
/// улогованог корисника у ревизиони дневник (функционалност 5.11). Сервисе није потребно
/// мијењати - нова акција у било ком контролеру се аутоматски прати.
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
            return; // анонимни захтјеви (нпр. пријава) се биљеже на другом мјесту

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
            // Неуспјешан упис у дневник не смије оборити захтјев који је већ успио.
            _logger.LogError(ex, "Упис у ревизиони дневник није успио за акцију {Action}.", action);
        }
    }

    /// <summary>
    /// ID ентитета: прво из одговора (нпр. новокреирани трошак има Id), иначе посљедњи
    /// Guid параметар из руте (нпр. userId код блокирања).
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
