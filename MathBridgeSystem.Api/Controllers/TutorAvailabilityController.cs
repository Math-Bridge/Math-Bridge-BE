using MathBridgeSystem.Application.DTOs.TutorAvailability;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/tutor-availabilities")]
    [ApiController]
    public class TutorAvailabilityController : ControllerBase
    {
        private readonly ITutorAvailabilityService _availabilityService;

        public TutorAvailabilityController(ITutorAvailabilityService availabilityService)
        {
            _availabilityService = availabilityService ?? throw new ArgumentNullException(nameof(availabilityService));
        }

        /// <summary>
        /// Create a new tutor availability
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "tutor,admin,staff")]
        public async Task<IActionResult> CreateAvailability([FromBody] CreateTutorAvailabilityRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // If user is tutor, verify they're creating for themselves
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    if (request.TutorId != userId)
                    {
                        return Forbid("Tutors can only create their own availabilities");
                    }
                }

                var availabilityId = await _availabilityService.CreateAvailabilityAsync(request);
                return CreatedAtAction(nameof(GetAvailabilityById), new { id = availabilityId }, new { availabilityId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing tutor availability
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "tutor,admin,staff")]
        public async Task<IActionResult> UpdateAvailability(Guid id, [FromBody] UpdateTutorAvailabilityRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // If user is tutor, verify they own this availability
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    var availability = await _availabilityService.GetAvailabilityByIdAsync(id);
                    
                    if (availability == null)
                        return NotFound(new { error = "Availability not found" });

                    if (availability.TutorId != userId)
                    {
                        return Forbid("Tutors can only update their own availabilities");
                    }
                }

                await _availabilityService.UpdateAvailabilityAsync(id, request);
                return Ok(new { message = "Availability updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a tutor availability
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "tutor,admin,staff")]
        public async Task<IActionResult> DeleteAvailability(Guid id)
        {
            try
            {
                // If user is tutor, verify they own this availability
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    var availability = await _availabilityService.GetAvailabilityByIdAsync(id);
                    
                    if (availability == null)
                        return NotFound(new { error = "Availability not found" });

                    if (availability.TutorId != userId)
                    {
                        return Forbid("Tutors can only delete their own availabilities");
                    }
                }

                await _availabilityService.DeleteAvailabilityAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get availability by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetAvailabilityById(Guid id)
        {
            try
            {
                var availability = await _availabilityService.GetAvailabilityByIdAsync(id);
                
                if (availability == null)
                    return NotFound(new { error = "Availability not found" });

                return Ok(availability);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all availabilities for a specific tutor
        /// </summary>
        [HttpGet("tutor/{tutorId}")]
        [Authorize(Roles = "tutor,parent,admin,staff")]
        public async Task<IActionResult> GetTutorAvailabilities(Guid tutorId, [FromQuery] bool activeOnly = true)
        {
            try
            {
                // If user is tutor, verify they're accessing their own availabilities
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    if (tutorId != userId)
                    {
                        return Forbid("Tutors can only view their own availabilities");
                    }
                }

                var availabilities = await _availabilityService.GetTutorAvailabilitiesAsync(tutorId, activeOnly);
                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get current user's availabilities (for logged-in tutor)
        /// </summary>
        [HttpGet("my-availabilities")]
        [Authorize(Roles = "tutor")]
        public async Task<IActionResult> GetMyAvailabilities([FromQuery] bool activeOnly = true)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var availabilities = await _availabilityService.GetTutorAvailabilitiesAsync(userId, activeOnly);
                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Search for available tutors based on criteria
        /// </summary>
        [HttpGet("search-tutors")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchAvailableTutors([FromQuery] SearchAvailableTutorsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tutors = await _availabilityService.SearchAvailableTutorsAsync(request);
                return Ok(tutors);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Check if availability conflicts with existing slots
        /// </summary>
        [HttpGet("check-conflict")]
        [Authorize(Roles = "tutor,admin,staff")]
        public async Task<IActionResult> CheckAvailabilityConflict(
            [FromQuery] Guid tutorId,
            [FromQuery] int dayOfWeek,
            [FromQuery] string startTime,
            [FromQuery] string endTime,
            [FromQuery] string effectiveFrom,
            [FromQuery] string effectiveUntil = null,
            [FromQuery] Guid? excludeAvailabilityId = null)
        {
            try
            {
                // If user is tutor, verify they're checking their own availability
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    if (tutorId != userId)
                    {
                        return Forbid("Tutors can only check their own availability conflicts");
                    }
                }

                var startTimeOnly = TimeOnly.Parse(startTime);
                var endTimeOnly = TimeOnly.Parse(endTime);
                var effectiveFromDate = DateOnly.Parse(effectiveFrom);
                DateOnly? effectiveUntilDate = string.IsNullOrEmpty(effectiveUntil) ? null : DateOnly.Parse(effectiveUntil);

                var hasConflict = await _availabilityService.CheckAvailabilityConflictAsync(
                    tutorId,
                    dayOfWeek,
                    startTimeOnly,
                    endTimeOnly,
                    effectiveFromDate,
                    effectiveUntilDate,
                    excludeAvailabilityId);

                return Ok(new { hasConflict });
            }
            catch (FormatException ex)
            {
                return BadRequest(new { error = "Invalid date or time format", details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update availability status
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "tutor,admin,staff")]
        public async Task<IActionResult> UpdateAvailabilityStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // If user is tutor, verify they own this availability
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    var availability = await _availabilityService.GetAvailabilityByIdAsync(id);
                    
                    if (availability == null)
                        return NotFound(new { error = "Availability not found" });

                    if (availability.TutorId != userId)
                    {
                        return Forbid("Tutors can only update their own availability status");
                    }
                }

                await _availabilityService.UpdateAvailabilityStatusAsync(id, request.Status);
                return Ok(new { message = "Status updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Bulk create multiple availabilities (e.g., weekly schedule)
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Roles = "tutor,admin,staff")]
        public async Task<IActionResult> BulkCreateAvailabilities([FromBody] List<CreateTutorAvailabilityRequest> requests)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // If user is tutor, verify they're creating for themselves
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    foreach (var request in requests)
                    {
                        if (request.TutorId != userId)
                        {
                            return Forbid("Tutors can only create their own availabilities");
                        }
                    }
                }

                var createdIds = await _availabilityService.BulkCreateAvailabilitiesAsync(requests);
                return CreatedAtAction(nameof(GetMyAvailabilities), new { }, new { createdIds, count = createdIds.Count });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // Helper DTO for status update
    public class UpdateStatusRequest
    {
        public string Status { get; set; }
    }
}