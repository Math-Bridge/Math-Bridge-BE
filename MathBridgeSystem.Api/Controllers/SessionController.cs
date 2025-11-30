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
        [Authorize(Roles = "parent,tutor")]
        public async Task<IActionResult> GetSessionsByChildId(Guid childId)
        {
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
    }
}