using MathBridgeSystem.Application.DTOs.DailyReport;
using MathBridgeSystem.Application.DTOs.Progress;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/daily-reports")]
    [ApiController]
    public class DailyReportController : ControllerBase
    {
        private readonly IDailyReportService _dailyReportService;

        public DailyReportController(IDailyReportService dailyReportService)
        {
            _dailyReportService = dailyReportService ?? throw new ArgumentNullException(nameof(dailyReportService));
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedAccessException("Missing or invalid user ID in token.");
            return userId;
        }

        /// <summary>
        /// Get a daily report by ID
        /// </summary>
        [HttpGet("{reportId}")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetDailyReportById(Guid reportId)
        {
            try
            {
                var dailyReport = await _dailyReportService.GetDailyReportByIdAsync(reportId);
                return Ok(dailyReport);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }


        /// <summary>
        /// Get all daily reports in the system
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "staff,admin")]
        public async Task<IActionResult> GetAllDailyReports()
        {
            try
            {
                var dailyReports = await _dailyReportService.GetAllDailyReportsAsync();
                return Ok(dailyReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving daily reports.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all daily reports for the logged-in tutor
        /// </summary>
        [HttpGet("tutor")]
        [Authorize(Roles = "tutor")]
        public async Task<IActionResult> GetDailyReportsByTutor()
        {
            var tutorId = GetUserId();
            try
            {
                var dailyReports = await _dailyReportService.GetDailyReportsByTutorIdAsync(tutorId);
                return Ok(dailyReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all daily reports for a specific child
        /// </summary>
        [HttpGet("child/{childId}")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetDailyReportsByChild(Guid childId)
        {
            try
            {
                var dailyReports = await _dailyReportService.GetDailyReportsByChildIdAsync(childId);
                return Ok(dailyReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all daily reports for a specific booking/session
        /// </summary>
        [HttpGet("booking/{bookingId}")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetDailyReportsByBooking(Guid bookingId)
        {
            try
            {
                var dailyReports = await _dailyReportService.GetDailyReportsByBookingIdAsync(bookingId);
                return Ok(dailyReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get learning completion forecast for a child
        /// Takes the unit from the oldest daily report and calculates when the child will complete all units
        /// Based on average 2 weeks per unit
        /// </summary>
        [HttpGet("contract/{contractId}/learning-forecast")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetLearningCompletionForecast(Guid contractId)
        {
            try
            {
                var forecast = await _dailyReportService.GetLearningCompletionForecastAsync(contractId);
                return Ok(forecast);
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
                return StatusCode(500, new { error = "An error occurred while retrieving the learning forecast.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get child unit progress from daily reports
        /// Returns information about which units the child has learned, how many times, and dates
        /// </summary>
        [HttpGet("contract/{contractId}/unit-progress")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetChildUnitProgress(Guid contractId)
        {
            try
            {
                var progress = await _dailyReportService.GetChildUnitProgressAsync(contractId);
                return Ok(progress);
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
                return StatusCode(500, new { error = "An error occurred while retrieving the child unit progress.", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new daily report
        /// Required fields: childId, bookingId, onTrack, haveHomework, unitId
        /// TutorId is automatically set from the logged-in user
        /// CreatedDate is automatically set to local time
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> CreateDailyReport([FromBody] CreateDailyReportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tutorId = GetUserId();
                var reportId = await _dailyReportService.CreateDailyReportAsync(request, tutorId);
                return CreatedAtAction(nameof(GetDailyReportById), new { reportId },
                    new { message = "Daily report created successfully", reportId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the daily report.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing daily report
        /// </summary>
        [HttpPut("{reportId}")]
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> UpdateDailyReport(Guid reportId, [FromBody] UpdateDailyReportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _dailyReportService.UpdateDailyReportAsync(reportId, request);
                return Ok(new { message = "Daily report updated successfully", reportId });
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
                return StatusCode(500, new { error = "An error occurred while updating the daily report.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a daily report
        /// </summary>
        [HttpDelete("{reportId}")]
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> DeleteDailyReport(Guid reportId)
        {
            try
            {
                var success = await _dailyReportService.DeleteDailyReportAsync(reportId);

                if (success)
                    return Ok(new { message = "Daily report deleted successfully" });

                return NotFound(new { error = "Daily report not found" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the daily report.", details = ex.Message });
            }
        }
        /// <summary>
        /// Get all daily reports for a specific contract (very useful for parent/staff to track progress per package)
        /// </summary>
        [HttpGet("contract/{contractId}")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetDailyReportsByContractId(Guid contractId)
        {
            try
            {
                var reports = await _dailyReportService.GetDailyReportsByContractIdAsync(contractId);
                return Ok(reports);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving daily reports.", details = ex.Message });
            }
        }
    }
}
