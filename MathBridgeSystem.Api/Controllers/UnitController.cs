using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/units")]
    [ApiController]
    public class UnitController : ControllerBase
    {
        private readonly IUnitService _unitService;

        public UnitController(IUnitService unitService)
        {
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        }

        /// <summary>
        /// Create a new unit
        /// </summary>
        /// <param name="request">Unit creation data</param>
        /// <returns>Created unit ID</returns>
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> CreateUnit([FromBody] CreateUnitRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Guid? createdBy = userIdClaim != null ? Guid.Parse(userIdClaim) : null;

                var unitId = await _unitService.CreateUnitAsync(request, createdBy);
                return CreatedAtAction(nameof(GetUnitById), new { id = unitId },
                    new { message = "Unit created successfully", unitId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the unit." });
            }
        }

        /// <summary>
        /// Update an existing unit
        /// </summary>
        /// <param name="id">Unit ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Success message</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] UpdateUnitRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Guid? updatedBy = userIdClaim != null ? Guid.Parse(userIdClaim) : null;

                await _unitService.UpdateUnitAsync(id, request, updatedBy);
                return Ok(new { message = "Unit updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Unit not found." });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the unit." });
            }
        }

        /// <summary>
        /// Delete a unit
        /// </summary>
        /// <param name="id">Unit ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DeleteUnit(Guid id)
        {
            try
            {
                await _unitService.DeleteUnitAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Unit not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the unit." });
            }
        }

        /// <summary>
        /// Get unit by ID
        /// </summary>
        /// <param name="id">Unit ID</param>
        /// <returns>Unit details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUnitById(Guid id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                    return NotFound(new { error = "Unit not found." });

                return Ok(unit);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the unit." });
            }
        }

        /// <summary>
        /// Get all units
        /// </summary>
        /// <returns>List of units</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllUnits()
        {
            try
            {
                var units = await _unitService.GetAllUnitsAsync();
                return Ok(new { data = units, totalCount = units.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving units." });
            }
        }
        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> GetAllActiveUnits()
        {
            try
            {
                var units = await _unitService.GetAllActiveUnitsAsync();
                return Ok(new { data = units, totalCount = units.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving units." });
            }
        }
        /// <summary>
        /// Get units by curriculum ID
        /// </summary>
        /// <param name="curriculumId">Curriculum ID</param>
        /// <returns>List of units for the curriculum</returns>
        [HttpGet("by-curriculum/{curriculumId}")]
        [Authorize]
        public async Task<IActionResult> GetUnitsByCurriculumId(Guid curriculumId)
        {
            try
            {
                var units = await _unitService.GetUnitsByCurriculumIdAsync(curriculumId);
                return Ok(new { data = units, totalCount = units.Count, curriculumId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving units." });
            }
        }

        /// <summary>
        /// Get units by contract ID
        /// </summary>
        /// <param name="contractId">Contract ID</param>
        /// <returns>List of units for the contract's curriculum</returns>
        [HttpGet("by-contract/{contractId}")]
        [Authorize]
        public async Task<IActionResult> GetUnitsByContractId(Guid contractId)
        {
            try
            {
                var units = await _unitService.GetUnitsByContractIdAsync(contractId);
                return Ok(new { data = units, totalCount = units.Count, contractId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving units." });
            }
        }

        /// <summary>
        /// Get unit by name
        /// </summary>
        /// <param name="name">Unit name</param>
        /// <returns>Unit details</returns>
        [HttpGet("by-name/{name}")]
        [Authorize]
        public async Task<IActionResult> GetUnitByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { error = "Unit name is required." });

                var unit = await _unitService.GetUnitByNameAsync(name);
                if (unit == null)
                    return NotFound(new { error = "Unit not found." });

                return Ok(unit);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the unit." });
            }
        }


        /// <summary>
        /// Get units by math concept ID
        /// </summary>
        /// <param name="conceptId">Math concept ID</param>
        /// <returns>List of units linked to the math concept</returns>
        [HttpGet("by-concept/{conceptId}")]
        [Authorize]
        public async Task<IActionResult> GetUnitsByMathConceptId(Guid conceptId)
        {
            try
            {
                var units = await _unitService.GetUnitsByMathConceptIdAsync(conceptId);
                return Ok(new { data = units, totalCount = units.Count, conceptId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving units." });
            }
        }
    }
}