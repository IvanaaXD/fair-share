using System.Security.Claims;
using FairShare.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FairShare.Api.Filters;

/// <summary>
/// Глобални филтер за стање налога улогованог корисника:
/// 1) блокиран корисник добија HTTP 403 на СВАКОМ endpoint-у, па и са старим, још важећим
///    токеном (функционалност 5.2 - блокирани корисник не може да користи систем);
/// 2) корисник са MustChangePassword = true добија HTTP 403 на свему осим endpoint-а
///    означених са [AllowWhilePasswordChangeRequired].
/// Анонимни endpoint-и (нпр. /api/auth/login) се не провјеравају.
/// </summary>
public class RequirePasswordChangeFilter : IAsyncActionFilter
{
    private readonly IUserRepository _userRepository;

    public RequirePasswordChangeFilter(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();

        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next();
            return;
        }

        var userIdValue = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdValue, out var userId))
        {
            var user = await _userRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);

            // НОВО: блокиран налог - важи и за endpoint-е дозвољене током промјене лозинке
            if (user is { IsBlocked: true })
            {
                context.Result = Forbidden("Ваш налог је блокиран. Обратите се администратору.");
                return;
            }

            var allowedDuringPasswordChange =
                endpoint?.Metadata?.GetMetadata<AllowWhilePasswordChangeRequiredAttribute>() is not null;

            if (user is { MustChangePassword: true } && !allowedDuringPasswordChange)
            {
                context.Result = Forbidden("Морате промијенити предефинисану лозинку прије наставка рада.");
                return;
            }
        }

        await next();
    }

    private static ObjectResult Forbidden(string message)
        => new(new { message }) { StatusCode = StatusCodes.Status403Forbidden };
}
