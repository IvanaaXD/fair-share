using System.Security.Claims;
using FairShare.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FairShare.Api.Filters;

/// <summary>
/// Глобални филтер: ако улоговани корисник има MustChangePassword = true, свака
/// радња осим оних означених са [AllowWhilePasswordChangeRequired] (и анонимних
/// endpointa попут /api/auth/login) враћа HTTP 403, у складу са функционалношћу 5.2
/// ("не дозволити му приступ апликацији док то не уради").
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

        var isExempt = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null
            || endpoint?.Metadata?.GetMetadata<AllowWhilePasswordChangeRequiredAttribute>() is not null;

        if (isExempt)
        {
            await next();
            return;
        }

        var userIdValue = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdValue, out var userId))
        {
            var user = await _userRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);
            if (user is { MustChangePassword: true })
            {
                context.Result = new ObjectResult(new
                {
                    message = "Морате промијенити предефинисану лозинку прије наставка рада."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }

        await next();
    }
}
