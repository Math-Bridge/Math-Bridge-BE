using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridge.Presentation.Controllers
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminController(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(currentUserRole))
                    return Unauthorized(new { error = "Role not found in token" });

                var userId = await _userService.AdminCreateUserAsync(request, currentUserRole);
                return Ok(new { userId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AdminCreateUser: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error while creating user" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(currentUserRole))
                    return Unauthorized(new { error = "Role not found in token" });

                var userId = await _userService.UpdateUserStatusAsync(id, request, currentUserRole);
                return Ok(new { userId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateUserStatus: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error while updating status" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}