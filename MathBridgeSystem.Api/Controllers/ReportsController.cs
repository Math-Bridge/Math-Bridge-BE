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
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }
            return userId;
        }

        /// <summary>
        /// Create a new report
        /// </summary>
        /// <param name="dto">Report creation data</param>
        /// <returns>Created report details</returns>
        [HttpPost]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Ensure parents can only create reports for themselves
                var currentUserId = GetCurrentUserId();

                var report = await _reportService.CreateReportAsync(dto,currentUserId);
                return CreatedAtAction(nameof(GetReportById), new { id = report.ReportId }, report);
            }
            catch (InvalidOperationException ex) // Rate limiting
            {
                 return StatusCode(429, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the report.", details = ex.Message });
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
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetReportById(Guid id)
        {
            try
            {
                var report = await _reportService.GetReportByIdAsync(id);
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
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "parent" && parentId != currentUserId)
                {
                     return Forbid("You can only view your own reports.");
                }

                var reports = await _reportService.GetReportsByParentIdAsync(parentId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving reports.", details = ex.Message });
            }
        }
    }
}