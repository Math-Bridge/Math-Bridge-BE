using MathBridgeSystem.Application.DTOs.TestResult;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/test-results")]
    [ApiController]
    public class TestResultController : ControllerBase
    {
        private readonly ITestResultService _testResultService;

        public TestResultController(ITestResultService testResultService)
        {
            _testResultService = testResultService ?? throw new ArgumentNullException(nameof(testResultService));
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
        /// Get a test result by ID
        /// </summary>
        [HttpGet("{resultId}")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetTestResultById(Guid resultId)
        {
            try
            {
                var testResult = await _testResultService.GetTestResultByIdAsync(resultId);
                return Ok(testResult);
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
        /// Get all test results for a tutor
        /// </summary>
        [HttpGet("tutor/{tutorId}")]
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> GetTestResultsByTutorId(Guid tutorId)
        {
            try
            {
                // Tutors can only view their own test results
                if (User.IsInRole("tutor"))
                {
                    var userId = GetUserId();
                    if (tutorId != userId)
                        return Forbid("You can only view your own test results.");
                }

                var testResults = await _testResultService.GetTestResultsByTutorIdAsync(tutorId);
                return Ok(testResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all test results for a child
        /// </summary>
        [HttpGet("child/{childId}")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetTestResultsByChildId(Guid childId)
        {
            try
            {
                var testResults = await _testResultService.GetTestResultsByChildIdAsync(childId);
                return Ok(testResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all test results for a curriculum
        /// </summary>
        [HttpGet("curriculum/{curriculumId}")]
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> GetTestResultsByCurriculumId(Guid curriculumId)
        {
            try
            {
                var testResults = await _testResultService.GetTestResultsByCurriculumIdAsync(curriculumId);
                return Ok(testResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get test result statistics for a child
        /// </summary>
        [HttpGet("child/{childId}/statistics")]
        [Authorize(Roles = "tutor,parent,staff,admin")]
        public async Task<IActionResult> GetChildStatistics(Guid childId)
        {
            try
            {
                var statistics = await _testResultService.GetChildStatisticsAsync(childId);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current tutor's test results
        /// </summary>
        [HttpGet("my-results")]
        [Authorize(Roles = "tutor")]
        public async Task<IActionResult> GetMyTestResults()
        {
            try
            {
                var tutorId = GetUserId();
                var testResults = await _testResultService.GetTestResultsByTutorIdAsync(tutorId);
                return Ok(testResults);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new test result
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "tutor")]
        public async Task<IActionResult> CreateTestResult([FromBody] CreateTestResultRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tutorId = GetUserId();
                var resultId = await _testResultService.CreateTestResultAsync(request, tutorId);
                return CreatedAtAction(nameof(GetTestResultById), new { resultId },
                    new { message = "Test result created successfully", resultId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the test result.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing test result
        /// </summary>
        [HttpPut("{resultId}")]
        [Authorize(Roles = "tutor")]
        public async Task<IActionResult> UpdateTestResult(Guid resultId, [FromBody] UpdateTestResultRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tutorId = GetUserId();
                await _testResultService.UpdateTestResultAsync(resultId, request, tutorId);
                return Ok(new { message = "Test result updated successfully", resultId });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the test result.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a test result
        /// </summary>
        [HttpDelete("{resultId}")]
        [Authorize(Roles = "tutor,admin")]
        public async Task<IActionResult> DeleteTestResult(Guid resultId)
        {
            try
            {
                var tutorId = GetUserId();
                var success = await _testResultService.DeleteTestResultAsync(resultId, tutorId);
                
                if (success)
                    return Ok(new { message = "Test result deleted successfully" });
                
                return NotFound(new { error = "Test result not found" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the test result.", details = ex.Message });
            }
        }
    }
}