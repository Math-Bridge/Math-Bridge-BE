using MathBridgeSystem.Application.DTOs.NotificationPreference;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/notification-preferences")]
    [ApiController]
    public class NotificationPreferenceController : ControllerBase
    {
        private readonly INotificationPreferenceService _preferenceService;

        public NotificationPreferenceController(INotificationPreferenceService preferenceService)
        {
            _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedAccessException("Missing or invalid user ID in token.");
            return userId;
        }

        /// <summary>
        /// Get notification preferences for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "parent,tutor,staff,admin")]
        public async Task<IActionResult> GetPreferencesByUserId(Guid userId)
        {
            try
            {
                // Users can only view their own preferences unless they are staff/admin
                if (!User.IsInRole("staff") && !User.IsInRole("admin"))
                {
                    var currentUserId = GetUserId();
                    if (userId != currentUserId)
                        return Forbid("You can only view your own notification preferences.");
                }

                var preferences = await _preferenceService.GetPreferencesByUserIdAsync(userId);
                return Ok(preferences);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current user's notification preferences
        /// </summary>
        [HttpGet("my-preferences")]
        [Authorize]
        public async Task<IActionResult> GetMyPreferences()
        {
            try
            {
                var userId = GetUserId();
                var preferences = await _preferenceService.GetOrCreateDefaultPreferencesAsync(userId);
                return Ok(preferences);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update notification preferences for a user
        /// </summary>
        [HttpPut("user/{userId}")]
        [Authorize(Roles = "parent,tutor,staff,admin")]
        public async Task<IActionResult> UpdatePreferences(Guid userId, [FromBody] UpdateNotificationPreferenceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Users can only update their own preferences unless they are staff/admin
                if (!User.IsInRole("staff") && !User.IsInRole("admin"))
                {
                    var currentUserId = GetUserId();
                    if (userId != currentUserId)
                        return Forbid("You can only update your own notification preferences.");
                }

                var preferenceId = await _preferenceService.CreateOrUpdatePreferencesAsync(userId, request);
                return Ok(new { message = "Notification preferences updated successfully", preferenceId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating preferences.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update current user's notification preferences
        /// </summary>
        [HttpPut("my-preferences")]
        [Authorize]
        public async Task<IActionResult> UpdateMyPreferences([FromBody] UpdateNotificationPreferenceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var preferenceId = await _preferenceService.CreateOrUpdatePreferencesAsync(userId, request);
                return Ok(new { message = "Notification preferences updated successfully", preferenceId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating preferences.", details = ex.Message });
            }
        }
    }
}