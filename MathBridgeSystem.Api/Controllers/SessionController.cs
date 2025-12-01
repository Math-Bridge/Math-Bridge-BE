using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/sessions")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedAccessException("Missing or invalid user ID in token.");
            return userId;
        }

        private bool IsStaff => User.IsInRole("staff");
        private bool IsTutor => User.IsInRole("tutor");

        // === PARENT APIs ===
        [HttpGet("parent")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> GetSessionsByParent()
        {
            var parentId = GetUserId();
            try
            {
                var sessions = await _sessionService.GetSessionsByParentAsync(parentId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{bookingId}")]
        [Authorize(Roles = "parent,tutor,staff")]
        public async Task<IActionResult> GetSessionById(Guid bookingId)
        {
            var userId = GetUserId();
            var role = User.IsInRole("staff") ? "staff" :
                       User.IsInRole("tutor") ? "tutor" : "parent";

            try
            {
                var session = await _sessionService.GetSessionByBookingIdAsync(bookingId, userId, role);
                if (session == null)
                    return NotFound();

                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("child/{childId}")]
        [Authorize(Roles = "parent,tutor,staff")]
        public async Task<IActionResult> GetSessionsByChildId(Guid childId)
        {
            var parentId = GetUserId();
            try
            {
                var sessions = await _sessionService.GetSessionsByChildIdAsync(childId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // === TUTOR API (TẤT CẢ BUỔI MÌNH DẠY) ===
        [HttpGet("tutor")]
        [Authorize(Roles = "staff,tutor")]
        public async Task<IActionResult> GetSessionsByTutor([FromQuery] Guid? tutorId)
        {
            var currentUserId = GetUserId();

            if (IsStaff && !tutorId.HasValue)
                return BadRequest(new { error = "tutorId is required for staff." });

            if (IsTutor && !IsStaff)
            {
                if (tutorId.HasValue && tutorId != currentUserId)
                    return Forbid("Tutor can only view their own schedule.");
                tutorId = currentUserId;
            }

            var targetTutorId = tutorId ?? currentUserId;

            try
            {
                var sessions = await _sessionService.GetSessionsByTutorIdAsync(targetTutorId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpPut("{bookingId}/tutor")]
        [Authorize(Roles = "staff")]
        public async Task<IActionResult> UpdateSessionTutor(Guid bookingId, [FromBody] UpdateSessionTutorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            try
            {
                var success = await _sessionService.UpdateSessionTutorAsync(bookingId, request.NewTutorId, userId);
                return Ok(new
                {
                    success,
                    message = "Session tutor updated successfully.",
                    bookingId,
                    newTutorId = request.NewTutorId
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        [HttpPut("{bookingId}/status")]
        [Authorize(Roles = "tutor,staff")]
        public async Task<IActionResult> UpdateSessionStatus(Guid bookingId, [FromBody] UpdateSessionStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            try
            {
                if (!IsStaff)
                {
                    var session = await _sessionService.GetSessionForTutorCheckAsync(bookingId, userId);
                    if (session == null)
                        return Forbid("You can only update your own sessions.");
                }

                var success = await _sessionService.UpdateSessionStatusAsync(bookingId, request.Status, userId);
                return Ok(new
                {
                    success,
                    message = $"Session status updated to '{request.Status}'."
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Session not found." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }
        /// <summary>
        /// Change tutor for a specific session
        /// Only staff can use this
        /// </summary>
        [HttpPut("change-tutor")]
        [Authorize(Roles = "staff,admin")]
        public async Task<IActionResult> ChangeSessionTutor([FromBody] ChangeSessionTutorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var staffId = GetUserId();

            try
            {
                await _sessionService.ChangeSessionTutorAsync(request, staffId);
                return Ok(new
                {
                    success = true,
                    message = $"Tutor changed successfully for session {request.BookingId}."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", details = ex.Message });
            }
        }
        /// <summary>
        /// Lấy danh sách tutor có thể thay thế cho buổi học này (ưu tiên SubTutor → tutor ngoài)
        /// </summary>
        [HttpGet("{bookingId}/replacement-tutors")]
        [Authorize(Roles = "staff,admin")]
        public async Task<IActionResult> GetReplacementTutors(Guid bookingId)
        {
            try
            {
                var data = await _sessionService.GetReplacementTutorsAsync(bookingId);
                return Ok(data);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", details = ex.Message });
            }
        }
        /// <summary>
        /// Get replacement plan when the Main Tutor is banned or inactive
        /// Prioritizes promoting a Substitute Tutor and suggests a new substitute from outside
        /// </summary>
        /// <param name="contractId">Contract ID</param>
        [HttpGet("{contractId}/main-tutor-replacement-plan")]
        [Authorize(Roles = "staff,admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMainTutorReplacementPlan(Guid contractId)
        {
            try
            {
                var plan = await _sessionService.GetMainTutorReplacementPlanAsync(contractId);

                return Ok(new
                {
                    success = true,
                    data = plan
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Replace the banned/inactive Main Tutor for the entire contract
        /// Updates Contract (MainTutorId + fills substitute slot) and all future sessions
        /// </summary>
        /// <param name="contractId">Contract ID</param>
        /// <param name="request">New Main Tutor and new Substitute Tutor</param>
        [HttpPut("{contractId}/main-tutor")]
        [Authorize(Roles = "staff,admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReplaceMainTutor(
            Guid contractId,
            [FromBody] ReplaceMainTutorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });

            try
            {
                var staffId = GetCurrentUserId();

                await _sessionService.ExecuteMainTutorReplacementAsync(
                    contractId,
                    request.NewMainTutorId,
                    request.NewSubstituteTutorId,
                    staffId);

                return Ok(new
                {
                    success = true,
                    message = "Main Tutor has been successfully replaced.",
                    data = new
                    {
                        contractId,
                        newMainTutorId = request.NewMainTutorId,
                        newSubstituteTutorId = request.NewSubstituteTutorId,
                        replacedAt = DateTime.UtcNow
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    details = ex.Message
                });
            }
        }

        // Helper: Get current staff ID from JWT
        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? User.FindFirst("userId")
                        ?? User.FindFirst("sub");

            if (claim == null || !Guid.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("User ID not found in token.");

            return userId;
        }
    }
}