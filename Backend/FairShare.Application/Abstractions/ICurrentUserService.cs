
namespace FairShare.Application.Abstractions
{
    public interface ICurrentUserService
    {
        string? Username { get; }
        Guid UserId { get; }
    }
}
