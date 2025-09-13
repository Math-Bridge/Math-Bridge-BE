using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MathBridge.Presentation.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = await _authService.RegisterAsync(request);
                return Ok(new { userId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Register: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during registration" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
        [HttpGet("verify-link")]
        public async Task<IActionResult> VerifyLink(string oobCode, string token)
        {
            try
            {
                var verifiedUserId = await _authService.VerifyEmailLinkAsync(oobCode, token);
                return Ok(new { userId = verifiedUserId, message = "Email verified and registration completed successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Verify: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during verification" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var jwtToken = await _authService.LoginAsync(request);
                return Ok(new { token = jwtToken });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Login: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during login" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (string.IsNullOrEmpty(request.IdToken))
                    return BadRequest(new { error = "Google ID token is required" });

                var token = await _authService.GoogleLoginAsync(request.IdToken);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GoogleLogin: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during Google login" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}