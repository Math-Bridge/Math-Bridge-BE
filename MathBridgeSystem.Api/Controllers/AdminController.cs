using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Presentation.Controllers
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
            {
                Console.WriteLine($"ModelState errors in CreateUser: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(currentUserRole))
                {
                    Console.WriteLine("CreateUser: Role not found in token");
                    return Unauthorized(new { error = "Role not found in token" });
                }

                var userId = await _userService.AdminCreateUserAsync(request, currentUserRole);
                return Ok(new { userId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateUser: {ex.ToString()}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error while creating user" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in UpdateUserStatus: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(currentUserRole))
                {
                    Console.WriteLine("UpdateUserStatus: Role not found in token");
                    return Unauthorized(new { error = "Role not found in token" });
                }

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