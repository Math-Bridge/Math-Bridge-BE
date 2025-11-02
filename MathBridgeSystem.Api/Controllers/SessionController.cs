using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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
            _sessionService = sessionService;
        }

        [HttpGet("parent")]
        public async Task<IActionResult> GetSessionsByParent()
        {
            var parentId = Guid.Parse(User.FindFirst("sub")?.Value!);
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
            var parentId = Guid.Parse(User.FindFirst("sub")?.Value!);
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
    }
}