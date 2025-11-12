// MathBridgeSystem.Api.Controllers/RescheduleController.cs
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/reschedule")]
    [ApiController]
    public class RescheduleController : ControllerBase
    {
        private readonly IRescheduleService _rescheduleService;

        public RescheduleController(IRescheduleService rescheduleService)
        {
            _rescheduleService = rescheduleService;
        }

        [HttpPost]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> Create([FromBody] CreateRescheduleRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }
            try
            {
                var result = await _rescheduleService.CreateRequestAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "staff")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveRescheduleRequestDto dto)
        {
            var staffId = Guid.Parse(User.FindFirst("sub")?.Value!);
            try
            {
                var result = await _rescheduleService.ApproveRequestAsync(staffId, id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "staff")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequestDto dto)
        {
            var staffId = Guid.Parse(User.FindFirst("sub")?.Value!);
            try
            {
                var result = await _rescheduleService.RejectRequestAsync(staffId, id, dto.Reason);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class RejectRequestDto
    {
        public string Reason { get; set; } = null!;
    }
}