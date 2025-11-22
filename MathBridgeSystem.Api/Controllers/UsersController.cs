using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(currentUserRole))
                {
                    Console.WriteLine("GetAllUsers: Role not found in token");
                    return Unauthorized(new { error = "Role not found in token" });
                }

                var users = await _userService.GetAllUsersAsync(currentUserRole);
                var userList = users.ToList();
                
                return Ok(new { data = userList, totalCount = userList.Count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllUsers: {ex}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "An error occurred while retrieving users." : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (string.IsNullOrEmpty(currentUserRole))
                {
                    Console.WriteLine("GetUserById: Role not found in token");
                    return Unauthorized(new { error = "Role not found in token" });
                }

                var user = await _userService.GetUserByIdAsync(id, currentUserId, currentUserRole);
                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserById: {ex}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "An error occurred while retrieving the user." : ex.Message;
                
                if (ex.Message.Contains("not found"))
                    return NotFound(new { error = errorMessage });
                
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Update user information
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Updated user ID</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors in UpdateUser: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (string.IsNullOrEmpty(currentUserRole))
                {
                    Console.WriteLine("UpdateUser: Role not found in token");
                    return Unauthorized(new { error = "Role not found in token" });
                }

                var userId = await _userService.UpdateUserAsync(id, request, currentUserId, currentUserRole);
                return Ok(new { userId, message = "User updated successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateUser: {ex}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "An error occurred while updating the user." : ex.Message;
                
                if (ex.Message.Contains("not found"))
                    return NotFound(new { error = errorMessage });
                
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Get wallet information for a parent
        /// </summary>
        /// <param name="parentId">Parent user ID</param>
        /// <returns>Wallet details with transaction history</returns>
        [HttpGet("{parentId}/wallet")]
        [Authorize(Roles = "admin,parent")]
        public async Task<IActionResult> GetWallet(Guid parentId)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (string.IsNullOrEmpty(currentUserRole))
                {
                    Console.WriteLine("GetWallet: Role not found in token");
                    return Unauthorized(new { error = "Role not found in token" });
                }

                var wallet = await _userService.GetWalletAsync(parentId, currentUserId, currentUserRole);
                return Ok(wallet);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetWallet: {ex}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "An error occurred while retrieving wallet information." : ex.Message;
                
                if (ex.Message.Contains("not found") || ex.Message.Contains("Invalid"))
                    return NotFound(new { error = errorMessage });
                
                return StatusCode(500, new { error = errorMessage });
            }
        }
        [HttpPost("{cid}/wallet/deduct")]
        public async Task<IActionResult> DeductWallet(Guid cid)
        {
            var id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("Invalid token"));
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("Invalid token"));
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(currentUserRole))
                    return Unauthorized(new { error = "Role not found in token" });

                var result = await _userService.DeductWalletAsync(id, cid, currentUserId, currentUserRole);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeductWallet: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error while deducting from wallet" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}

