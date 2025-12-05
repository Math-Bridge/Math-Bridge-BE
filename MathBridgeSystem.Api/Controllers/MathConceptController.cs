using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/math-concepts")]
    [ApiController]
    public class MathConceptController : ControllerBase
    {
        private readonly IMathConceptService _mathConceptService;

        public MathConceptController(IMathConceptService mathConceptService)
        {
            _mathConceptService = mathConceptService ?? throw new ArgumentNullException(nameof(mathConceptService));
        }

        /// <summary>
        /// Create a new math concept
        /// </summary>
        /// <param name="request">Math concept creation data</param>
        /// <returns>Created math concept ID</returns>
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> CreateMathConcept([FromBody] CreateMathConceptRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var conceptId = await _mathConceptService.CreateMathConceptAsync(request);
                return CreatedAtAction(nameof(GetMathConceptById), new { id = conceptId },
                    new { message = "Math concept created successfully", conceptId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the math concept." });
            }
        }

        /// <summary>
        /// Update an existing math concept
        /// </summary>
        /// <param name="id">Math concept ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Success message</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateMathConcept(Guid id, [FromBody] UpdateMathConceptRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _mathConceptService.UpdateMathConceptAsync(id, request);
                return Ok(new { message = "Math concept updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Math concept not found." });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the math concept." });
            }
        }

        /// <summary>
        /// Delete a math concept
        /// </summary>
        /// <param name="id">Math concept ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DeleteMathConcept(Guid id)
        {
            try
            {
                await _mathConceptService.DeleteMathConceptAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Math concept not found." });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("linked to"))
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the math concept." });
            }
        }

        /// <summary>
        /// Get math concept by ID
        /// </summary>
        /// <param name="id">Math concept ID</param>
        /// <returns>Math concept details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetMathConceptById(Guid id)
        {
            try
            {
                var mathConcept = await _mathConceptService.GetMathConceptByIdAsync(id);
                if (mathConcept == null)
                    return NotFound(new { error = "Math concept not found." });

                return Ok(mathConcept);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the math concept." });
            }
        }

        /// <summary>
        /// Get all math concepts
        /// </summary>
        /// <returns>List of math concepts</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllMathConcepts()
        {
            try
            {
                var mathConcepts = await _mathConceptService.GetAllMathConceptsAsync();
                return Ok(new { data = mathConcepts, totalCount = mathConcepts.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving math concepts." });
            }
        }

        /// <summary>
        /// Get math concepts by unit ID
        /// </summary>
        /// <param name="unitId">Unit ID</param>
        /// <returns>List of math concepts for the unit</returns>
        [HttpGet("by-unit/{unitId}")]
        [Authorize]
        public async Task<IActionResult> GetMathConceptsByUnitId(Guid unitId)
        {
            try
            {
                var mathConcepts = await _mathConceptService.GetMathConceptsByUnitIdAsync(unitId);
                return Ok(new { data = mathConcepts, totalCount = mathConcepts.Count, unitId });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving math concepts." });
            }
        }

        /// <summary>
        /// Get math concept by name
        /// </summary>
        /// <param name="name">Math concept name</param>
        /// <returns>Math concept details</returns>
        [HttpGet("by-name/{name}")]
        [Authorize]
        public async Task<IActionResult> GetMathConceptByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { error = "Math concept name is required." });

                var mathConcept = await _mathConceptService.GetMathConceptByNameAsync(name);
                if (mathConcept == null)
                    return NotFound(new { error = "Math concept not found." });

                return Ok(mathConcept);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the math concept." });
            }
        }

        /// <summary>
        /// Get math concepts by category
        /// </summary>
        /// <param name="category">Category name</param>
        /// <returns>List of math concepts in the category</returns>
        [HttpGet("by-category/{category}")]
        [Authorize]
        public async Task<IActionResult> GetMathConceptsByCategory(string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                    return BadRequest(new { error = "Category is required." });

                var mathConcepts = await _mathConceptService.GetMathConceptsByCategoryAsync(category);
                return Ok(new { data = mathConcepts, totalCount = mathConcepts.Count, category });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving math concepts." });
            }
        }

        /// <summary>
        /// Link math concept to units
        /// </summary>
        /// <param name="conceptId">Math concept ID</param>
        /// <param name="request">List of unit IDs to link</param>
        /// <returns>Success message</returns>
        [HttpPost("{conceptId}/link-units")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> LinkMathConceptToUnits(Guid conceptId, [FromBody] LinkMathConceptToUnitsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _mathConceptService.LinkMathConceptToUnitsAsync(conceptId, request.UnitIds);
                return Ok(new { message = "Math concept linked to units successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while linking math concept to units." });
            }
        }

        /// <summary>
        /// Unlink math concept from units
        /// </summary>
        /// <param name="conceptId">Math concept ID</param>
        /// <param name="request">List of unit IDs to unlink</param>
        /// <returns>Success message</returns>
        [HttpPost("{conceptId}/unlink-units")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UnlinkMathConceptFromUnits(Guid conceptId, [FromBody] UnlinkMathConceptFromUnitsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _mathConceptService.UnlinkMathConceptFromUnitsAsync(conceptId, request.UnitIds);
                return Ok(new { message = "Math concept unlinked from units successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while unlinking math concept from units." });
            }
        }
    }
}
