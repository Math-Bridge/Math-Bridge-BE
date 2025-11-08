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
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> GetSessionById(Guid bookingId)
        {
            var parentId = GetUserId();
            try
            {
                var session = await _sessionService.GetSessionByIdAsync(bookingId, parentId);
                if (session == null) return NotFound();
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("child/{childId}")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> GetSessionsByChildId(Guid childId)
        {
            var parentId = GetUserId();
            try
            {
                var sessions = await _sessionService.GetSessionsByChildIdAsync(childId, parentId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // === TUTOR APIs (MAIN) ===
        [HttpGet("tutor/main")]
        [Authorize(Roles = "staff,tutor")]
        public async Task<IActionResult> GetSessionsByMainTutor([FromQuery] Guid? tutorId)
        {
            var currentUserId = GetUserId();

         
            if (IsStaff && !tutorId.HasValue)
                return BadRequest(new { error = "tutorId is required for staff." });

         
            if (IsTutor && !IsStaff)
            {
                if (tutorId.HasValue && tutorId != currentUserId)
                    return Forbid("Tutor can only view their own main schedule.");

                tutorId = currentUserId;
            }

            var targetTutorId = tutorId ?? currentUserId;

            try
            {
                var sessions = await _sessionService.GetSessionsByMainTutorIdAsync(targetTutorId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // === TUTOR APIs (SUBSTITUTE) ===
        [HttpGet("tutor/substitute")]
        [Authorize(Roles = "staff,tutor")]
        public async Task<IActionResult> GetSessionsBySubstituteTutor([FromQuery] Guid? tutorId)
        {
            var currentUserId = GetUserId();

            // STAFF: BẮT BUỘC TRUYỀN tutorId
            if (IsStaff && !tutorId.HasValue)
                return BadRequest(new { error = "tutorId is required for staff." });

            // TUTOR: Chỉ được xem chính mình
            if (IsTutor && !IsStaff)
            {
                if (tutorId.HasValue && tutorId != currentUserId)
                    return Forbid("Tutor can only view their own substitute schedule.");

                tutorId = currentUserId;
            }

            var targetTutorId = tutorId ?? currentUserId;

            try
            {
                var sessions = await _sessionService.GetSessionsBySubstituteTutorIdAsync(targetTutorId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                // Staff bỏ qua kiểm tra quyền tutor
                if (!IsStaff)
                {
                    // GỌI SERVICE ĐỂ KIỂM TRA
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