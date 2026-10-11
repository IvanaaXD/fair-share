
namespace FairShare.Application.DTOs.Auth
{
    public class AuthResponse
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required string RefreshToken { get; set; }
        public required string AccessToken { get; set; }
    }
}
