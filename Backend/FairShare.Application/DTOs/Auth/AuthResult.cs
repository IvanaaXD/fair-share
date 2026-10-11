
namespace FairShare.Application.DTOs.Auth
{
    public class AuthResult
    {
        public int StatusCode { get; set; }
        public required AuthResponse Data { get; set; }
    }
}
