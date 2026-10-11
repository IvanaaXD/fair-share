using System.ComponentModel;
using System.Security.Claims;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Caching;
using FairShare.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FairShare.Api.Filters;

/// <summary>
/// Global filter for the state of the signed-in user's account:
/// 1) a blocked user gets HTTP 403 on EVERY endpoint, even with an old, still valid token
///    (feature 5.2 - a blocked user cannot use the system);
/// 2) a user with MustChangePassword = true gets HTTP 403 on everything except endpoints
///    marked with [AllowWhilePasswordChangeRequired].
/// Anonymous endpoints (e.g. /api/auth/login) are not checked.
///
/// The account state is cached. This filter runs on every request, so before it
/// meant one extra database query per request. Blocking a user or changing the password
/// saves the User row, which invalidates the cached state immediately (CacheInvalidationInterceptor),
/// so a block still takes effect on the very next request.
/// </summary>
public class RequirePasswordChangeFilter : IAsyncActionFilter
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cache;

    public RequirePasswordChangeFilter(IUserRepository userRepository, ICacheService cache)
    {
        _userRepository = userRepository;
        _cache = cache;
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
            var status = await GetAccountStatusAsync(userId, context.HttpContext.RequestAborted);

            // A blocked account - applies to endpoints allowed during a password change as well.
            if (status is { IsBlocked: true })
            {
                context.Result = Forbidden("Your account is blocked. Contact the administrator.");
                return;
            }

            var allowedDuringPasswordChange =
                endpoint?.Metadata?.GetMetadata<AllowWhilePasswordChangeRequiredAttribute>() is not null;

            if (status is { MustChangePassword: true } && !allowedDuringPasswordChange)
            {
                context.Result = Forbidden("You must change your predefined password before continuing.");
                return;
            }
        }

        await next();
    }

    private Task<AccountStatus?> GetAccountStatusAsync(Guid userId, CancellationToken cancellationToken)
        => _cache.GetOrCreateAsync<AccountStatus?>(
            CacheKeys.AccountStatus(userId),
            async token =>
            {
                var user = await _userRepository.GetByIdAsync(userId, token);
                return user is null ? null : new AccountStatus(user.IsBlocked, user.MustChangePassword);
            },
            CacheDurations.AccountStatus,
            new[] { CacheTags.User(userId) },
            cancellationToken);

    private static ObjectResult Forbidden(string message)
        => new(new { message }) { StatusCode = StatusCodes.Status403Forbidden };

    /// <summary>
    /// Only the two flags the filter needs are cached - never the whole User (password hash...).
    /// Sealed + ImmutableObject lets HybridCache hand out the same instance to every request
    /// instead of copying it each time.
    /// </summary>
    [ImmutableObject(true)]
    public sealed record AccountStatus(bool IsBlocked, bool MustChangePassword);
}
