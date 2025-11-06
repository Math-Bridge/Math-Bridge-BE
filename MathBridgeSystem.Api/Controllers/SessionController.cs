using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/sessions")]
    [ApiController]
    [Authorize(Roles = "parent")]
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

        [HttpGet("parent")]
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
    }
}