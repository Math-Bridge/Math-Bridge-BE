using MathBridgeSystem.Application.DTOs.Report;
using MathBridgeSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("User ID not found in claims.");
            return userId;
        }

        private int GetCurrentUserRoleId()
        {
            var roleClaim = User.FindFirst("RoleId") ?? User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedAccessException("Role not found in claims.");

            // Handle role name to roleId mapping if needed
            if (int.TryParse(roleClaim.Value, out var roleId))
                return roleId;

            // Map role names to IDs
            return roleClaim.Value.ToLower() switch
            {
                "tutor" => 2,
                "parent" => 3,
                "staff" => 4,
                "admin" => 1,
                _ => throw new UnauthorizedAccessException($"Unknown role: {roleClaim.Value}")
            };
        }

        /// <summary>
        /// Create a new report.
        /// Type is automatically set based on the creator's role:
        /// - Tutor (RoleId 2) ? Type = "tutor"
        /// - Parent (RoleId 3) ? Type = "parent"
        /// </summary>
        /// <param name="dto">Report creation data (TutorId required for parent reports)</param>
        /// <returns>Created report details</returns>
        [HttpPost]
        [Authorize(Roles = "tutor,parent")]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = GetCurrentUserId();
                var roleId = GetCurrentUserRoleId();

                var report = await _reportService.CreateReportAsync(dto, currentUserId, roleId);
                return CreatedAtAction(nameof(GetReportById), new { id = report.ReportId }, report);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(429, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the report.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing report.
        /// Only the report creator can update their own report.
        /// Type cannot be modified.
        /// Only pending reports can be updated.
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <param name="dto">Update data (Content and/or Url)</param>
        /// <returns>Updated report details</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "tutor,parent")]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = GetCurrentUserId();
                var report = await _reportService.UpdateReportAsync(id, dto, currentUserId);
                return Ok(report);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the report.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a report
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
            try
            {
                await _reportService.DeleteReportAsync(id);
                return Ok(new { message = "Report deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the report.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update report status (Approve/Deny)
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <param name="dto">Status update data</param>
        /// <returns>Updated report details</returns>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateReportStatus(Guid id, [FromBody] UpdateReportStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var report = await _reportService.UpdateStatusAsync(id, dto);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the report status.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get report by ID
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <returns>Report details</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "admin,staff,tutor,parent")]
        public async Task<IActionResult> GetReportById(Guid id)
        {
            try
            {
                var report = await _reportService.GetReportByIdAsync(id);

                // For tutor/parent, only allow viewing their own reports
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value?.ToLower();
                if (userRole == "tutor" || userRole == "parent")
                {
                    var currentUserId = GetCurrentUserId();
                    if (report.ParentId != currentUserId && report.TutorId != currentUserId)
                        return Forbid("You can only view reports you are involved in.");
                }

                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the report.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all reports
        /// </summary>
        /// <returns>List of all reports</returns>
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetAllReports()
        {
            try
            {
                var reports = await _reportService.GetAllReportsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving reports.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get reports by Parent ID
        /// </summary>
        /// <param name="parentId">Parent ID</param>
        /// <returns>List of reports</returns>
        [HttpGet("parent/{parentId}")]
        [Authorize(Roles = "admin,staff,parent")]
        public async Task<IActionResult> GetReportsByParentId(Guid parentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value?.ToLower();

                if (userRole == "parent" && parentId != currentUserId)
                    return Forbid("You can only view your own reports.");

                var reports = await _reportService.GetReportsByParentIdAsync(parentId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving reports.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get reports by Tutor ID
        /// </summary>
        /// <param name="tutorId">Tutor ID</param>
        /// <returns>List of reports</returns>
        [HttpGet("tutor/{tutorId}")]
        [Authorize(Roles = "admin,staff,tutor")]
        public async Task<IActionResult> GetReportsByTutorId(Guid tutorId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value?.ToLower();

                if (userRole == "tutor" && tutorId != currentUserId)
                    return Forbid("You can only view your own reports.");

                var reports = await _reportService.GetReportsByTutorIdAsync(tutorId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving reports.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get reports for the currently logged-in tutor
        /// </summary>
        /// <returns>List of reports involving the tutor</returns>
        [HttpGet("tutor/me")]
        [Authorize(Roles = "tutor")]
        public async Task<IActionResult> GetMyTutorReports()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var reports = await _reportService.GetReportsByTutorIdAsync(currentUserId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving reports.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get reports for the currently logged-in parent
        /// </summary>
        /// <returns>List of reports created by the parent</returns>
        [HttpGet("parent/me")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> GetMyParentReports()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var reports = await _reportService.GetReportsByParentIdAsync(currentUserId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving reports.", details = ex.Message });
            }
        }
    }
}