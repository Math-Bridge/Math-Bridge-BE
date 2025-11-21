using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Presentation.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IMemoryCache _cache;

        public AuthController(IAuthService authService, IMemoryCache cache)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in Register: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var message = await _authService.RegisterAsync(request);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Register: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during registration" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }


        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in ResendVerification: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var message = await _authService.ResendVerificationAsync(request);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ResendVerification: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during resend verification" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in VerifyEmail: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                if (string.IsNullOrEmpty(request.OobCode))
                {
                    Console.WriteLine("OobCode is null or empty");
                    return BadRequest(new { error = "OobCode is required" });
                }

                var verifiedUserId = await _authService.VerifyEmailAsync(request.OobCode);
                return Ok(new { userId = verifiedUserId, message = "Email verified and registration completed successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in VerifyEmail: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during verification" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmailGet(string oobCode)
        {
            if (string.IsNullOrEmpty(oobCode))
            {
                Console.WriteLine("OobCode is null or empty in GET");
                return BadRequest(new { error = "OobCode is required" });
            }

            try
            {
                var verifiedUserId = await _authService.VerifyEmailAsync(oobCode);
                return Ok("Email verified successfully! You can now login.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in VerifyEmailGet: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during verification" : ex.Message;
                return BadRequest(new { error = errorMessage });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in Login: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(new { token = result.Token, userId = result.UserId, role = result.Role, roleId = result.RoleId });
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
            {
                Console.WriteLine($"ModelState errors in GoogleLogin: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

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
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in ForgotPassword: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var message = await _authService.ForgotPasswordAsync(request);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ForgotPassword: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during forgot password request" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }

        [HttpGet("verify-reset")]
        public IActionResult VerifyResetGet(string oobCode)
        {
            if (string.IsNullOrEmpty(oobCode))
            {
                Console.WriteLine("OobCode is null or empty in VerifyResetGet");
                return BadRequest(new { error = "OobCode is required" });
            }

            try
            {
                if (!_cache.TryGetValue(oobCode, out _))
                {
                    Console.WriteLine($"VerifyResetGet: Invalid or expired oobCode: {oobCode}");
                    throw new Exception("Invalid or expired reset code");
                }
                // In a real app, this would redirect to a frontend page to input new password.
                // For testing, return a message to proceed with POST /reset-password.
                return Ok(new { message = "Reset link is valid. Use POST /api/auth/reset-password with oobCode and newPassword." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in VerifyResetGet: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during reset verification" : ex.Message;
                return BadRequest(new { error = errorMessage });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in ResetPassword: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var message = await _authService.ResetPasswordAsync(request);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ResetPassword: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during password reset" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in ChangePassword: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("Invalid token"));
                var message = await _authService.ChangePasswordAsync(request, userId);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ChangePassword: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error during password change" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}