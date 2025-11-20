using MathBridgeSystem.Application.DTOs.TestResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using MathBridgeSystem.Application.Interfaces;

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
        /// Get all test results for a contract
        /// </summary>
        [HttpGet("contract/{contractId}")]
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> GetTestResultsByContractId(Guid contractId)
        {
            try
            {
                var testResults = await _testResultService.GetTestResultsByContractIdAsync(contractId);
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
        [Authorize(Roles = "parent,tutor,staff,admin")]
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
        /// Create a new test result
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> CreateTestResult([FromBody] CreateTestResultRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var resultId = await _testResultService.CreateTestResultAsync(request);
                return CreatedAtAction(nameof(GetTestResultById), new { resultId },
                    new { message = "Test result created successfully", resultId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
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
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> UpdateTestResult(Guid resultId, [FromBody] UpdateTestResultRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _testResultService.UpdateTestResultAsync(resultId, request);
                return Ok(new { message = "Test result updated successfully", resultId });
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
                return StatusCode(500, new { error = "An error occurred while updating the test result.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a test result
        /// </summary>
        [HttpDelete("{resultId}")]
        [Authorize(Roles = "tutor,staff,admin")]
        public async Task<IActionResult> DeleteTestResult(Guid resultId)
        {
            try
            {
                var success = await _testResultService.DeleteTestResultAsync(resultId);
                
                if (success)
                    return Ok(new { message = "Test result deleted successfully" });
                
                return NotFound(new { error = "Test result not found" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the test result.", details = ex.Message });
            }
        }
    }
}

