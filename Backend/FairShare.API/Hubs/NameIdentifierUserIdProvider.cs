using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace FairShare.API.Hubs;

/// <summary>
/// Which user a SignalR connection belongs to: the same claim the rest of the API uses for the
/// current user (NameIdentifier), with "sub" as a fallback when claim mapping is turned off.
/// </summary>
public class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? connection.User?.FindFirst("sub")?.Value;
}
