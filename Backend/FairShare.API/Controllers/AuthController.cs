using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _identityService.LoginAsync(request);

            if (result.StatusCode == 401)
            {
                return Unauthorized(new { message = "Incorrect credentials." });
            }
            else if (result.StatusCode == 404)
            {
                return NotFound(new { message = "Username does not exist." });
            }
            else
            {
                return Ok(result);
            }
        }
    }
}