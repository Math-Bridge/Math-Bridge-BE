﻿// MathBridgeSystem.Api.Controllers/RescheduleController.cs
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

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }
            return userId;
        }

        [HttpGet]
        [Authorize(Roles = "staff,parent")]
        public async Task<IActionResult> GetAll([FromQuery] Guid? parentId = null)
        {
            try
            {
                var userId = GetUserId();
                var role = User.IsInRole("staff") ? "staff" : "parent";

                // If parent role, can only see their own requests
                if (role == "parent")
                {
                    parentId = userId;
                }

                var requests = await _rescheduleService.GetAllAsync(parentId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "staff,parent")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var role = User.IsInRole("staff") ? "staff" : "parent";

                var request = await _rescheduleService.GetByIdAsync(id, userId, role);
                if (request == null)
                    return NotFound(new { error = "Reschedule request not found" });

                return Ok(request);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> Create([FromBody] CreateRescheduleRequestDto dto)
        {
            var userId = GetUserId();
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
            var userId = GetUserId();
            try
            {
                var result = await _rescheduleService.ApproveRequestAsync(userId, id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{rescheduleRequestId}/available-sub-tutors")]
        [Authorize]
        public async Task<IActionResult> GetAvailableSubTutors(Guid rescheduleRequestId)
        {
            try
            {
                var result = await _rescheduleService.GetAvailableSubTutorsAsync(rescheduleRequestId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
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
            var userId = GetUserId();
            try
            {
                var result = await _rescheduleService.RejectRequestAsync(userId, id, dto.Reason);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Cancel a session and refund money to parent wallet. Optionally approve a reschedule request.
        /// </summary>
        [HttpPost("cancel-session/{sessionId}")]
        [Authorize(Roles = "staff,admin,parent")]
        public async Task<IActionResult> CancelSessionAndRefund(Guid sessionId, [FromQuery] Guid rescheduleRequestId )
        {
            try
            {
                var result = await _rescheduleService.CancelSessionAndRefundAsync(sessionId, rescheduleRequestId);
                return Ok(result);
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
                return StatusCode(500, new { error = "An error occurred while cancelling the session.", details = ex.Message });
            }
        }
        /// <summary>
        /// Tutor bận → gửi yêu cầu cho Staff chọn SubTutor thay thế (không đổi ngày giờ)
        /// </summary>
        [HttpPost("tutor-replacement")]
        [Authorize(Roles = "tutor")]
        public async Task<IActionResult> RequestTutorReplacement([FromBody] TutorReplacementRequest request)
        {
            var tutorId = GetUserId();

            try
            {
                var result = await _rescheduleService.CreateTutorReplacementRequestAsync(
                    request.BookingId,
                    tutorId,
                    request.Reason ?? "I'm too busy to teach this session."
                );

                return Ok(new
                {
                    success = true,
                    message = "The request to replace the tutor has been successfully submitted! Staff will select a replacement as soon as possible.",
                    data = result
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "No lesson found." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "System error", details = ex.Message });
            }
        }
    }

    public class RejectRequestDto
    {
        public string Reason { get; set; } = null!;
    }

    public class TutorReplacementRequest
    {
        public Guid BookingId { get; set; }
        public string? Reason { get; set; }
    }
}