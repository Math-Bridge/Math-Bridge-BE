using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/math-programs")]
    [ApiController]
    public class MathProgramController : ControllerBase
    {
        private readonly IMathProgramService _mathProgramService;

        public MathProgramController(IMathProgramService mathProgramService)
        {
            _mathProgramService = mathProgramService ?? throw new ArgumentNullException(nameof(mathProgramService));
        }

        /// <summary>
        /// Create a new math program
        /// </summary>
        /// <param name="request">Math program creation data</param>
        /// <returns>Created program ID</returns>
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> CreateMathProgram([FromBody] CreateMathProgramRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var programId = await _mathProgramService.CreateMathProgramAsync(request);
                return CreatedAtAction(nameof(GetMathProgramById), new { id = programId },
                    new { message = "Math program created successfully", programId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the math program." });
            }
        }

        /// <summary>
        /// Update an existing math program
        /// </summary>
        /// <param name="id">Program ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Success message</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateMathProgram(Guid id, [FromBody] UpdateMathProgramRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _mathProgramService.UpdateMathProgramAsync(id, request);
                return Ok(new { message = "Math program updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Math program not found." });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the math program." });
            }
        }

        /// <summary>
        /// Delete a math program (only if no packages or test results)
        /// </summary>
        /// <param name="id">Program ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteMathProgram(Guid id)
        {
            try
            {
                await _mathProgramService.DeleteMathProgramAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Math program not found." });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot delete"))
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the math program." });
            }
        }

        /// <summary>
        /// Get math program by ID
        /// </summary>
        /// <param name="id">Program ID</param>
        /// <returns>Math program details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetMathProgramById(Guid id)
        {
            try
            {
                var mathProgram = await _mathProgramService.GetMathProgramByIdAsync(id);
                if (mathProgram == null)
                    return NotFound(new { error = "Math program not found." });

                return Ok(mathProgram);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the math program." });
            }
        }

        /// <summary>
        /// Get all math programs
        /// </summary>
        /// <returns>List of math programs</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMathPrograms()
        {
            try
            {
                var mathPrograms = await _mathProgramService.GetAllMathProgramsAsync();
                return Ok(new { data = mathPrograms, totalCount = mathPrograms.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving math programs." });
            }
        }

        /// <summary>
        /// Get math program by name
        /// </summary>
        /// <param name="name">Program name</param>
        /// <returns>Math program details</returns>
        [HttpGet("by-name/{name}")]
        [Authorize]
        public async Task<IActionResult> GetMathProgramByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { error = "Program name is required." });

                var mathProgram = await _mathProgramService.GetMathProgramByNameAsync(name);
                if (mathProgram == null)
                    return NotFound(new { error = "Math program not found." });

                return Ok(mathProgram);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the math program." });
            }
        }
    }
}