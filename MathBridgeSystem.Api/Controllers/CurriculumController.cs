using MathBridgeSystem.Application.DTOs.Curriculum;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/curricula")]
    [ApiController]
    public class CurriculumController : ControllerBase
    {
        private readonly ICurriculumService _curriculumService;

        public CurriculumController(ICurriculumService curriculumService)
        {
            _curriculumService = curriculumService ?? throw new ArgumentNullException(nameof(curriculumService));
        }

        /// <summary>
        /// Create a new curriculum
        /// </summary>
        /// <param name="request">Curriculum creation data</param>
        /// <returns>Created curriculum ID</returns>
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> CreateCurriculum([FromBody] CreateCurriculumRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var curriculumId = await _curriculumService.CreateCurriculumAsync(request);
                return CreatedAtAction(nameof(GetCurriculumById), new { id = curriculumId },
                    new { message = "Curriculum created successfully", curriculumId });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the curriculum.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing curriculum
        /// </summary>
        /// <param name="id">Curriculum ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Success message</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateCurriculum(Guid id, [FromBody] UpdateCurriculumRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _curriculumService.UpdateCurriculumAsync(id, request);
                return Ok(new { message = "Curriculum updated successfully", curriculumId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the curriculum.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a curriculum (only if no associated schools or packages)
        /// </summary>
        /// <param name="id">Curriculum ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DeleteCurriculum(Guid id)
        {
            try
            {
                await _curriculumService.DeleteCurriculumAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("associated"))
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the curriculum.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get curriculum by ID
        /// </summary>
        /// <param name="id">Curriculum ID</param>
        /// <returns>Curriculum details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetCurriculumById(Guid id)
        {
            try
            {
                var curriculum = await _curriculumService.GetCurriculumByIdAsync(id);
                if (curriculum == null)
                    return NotFound(new { error = "Curriculum not found." });

                return Ok(curriculum);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the curriculum.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all curricula with pagination
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of curricula with pagination</returns>
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetAllCurricula([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var curricula = await _curriculumService.GetAllCurriculaAsync();
                var totalCount = curricula.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var pagedCurricula = curricula
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    data = pagedCurricula,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages,
                        hasNext = page < totalPages,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving curricula.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get active curricula (public access)
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of active curricula</returns>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveCurricula([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var curricula = await _curriculumService.GetActiveCurriculaAsync();
                var totalCount = curricula.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var pagedCurricula = curricula
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    data = pagedCurricula,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages,
                        hasNext = page < totalPages,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving active curricula.", details = ex.Message });
            }
        }

        /// <summary>
        /// Search curricula with filters and pagination
        /// </summary>
        /// <param name="request">Search criteria</param>
        /// <returns>Filtered list of curricula</returns>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchCurricula([FromQuery] CurriculumSearchRequest request)
        {
            try
            {
                var curricula = await _curriculumService.SearchCurriculaAsync(request);
                var totalCount = await _curriculumService.GetCurriculaCountAsync(request);
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

                return Ok(new
                {
                    data = curricula,
                    pagination = new
                    {
                        currentPage = request.Page,
                        pageSize = request.PageSize,
                        totalCount,
                        totalPages,
                        hasNext = request.Page < totalPages,
                        hasPrevious = request.Page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while searching curricula.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get curriculum with associated schools
        /// </summary>
        /// <param name="id">Curriculum ID</param>
        /// <returns>Curriculum with schools</returns>
        [HttpGet("{id}/with-schools")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetCurriculumWithSchools(Guid id)
        {
            try
            {
                var curriculum = await _curriculumService.GetCurriculumWithSchoolsAsync(id);
                if (curriculum == null)
                    return NotFound(new { error = "Curriculum not found." });

                return Ok(curriculum);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the curriculum with schools.", details = ex.Message });
            }
        }

        /// <summary>
        /// Activate a curriculum
        /// </summary>
        /// <param name="id">Curriculum ID</param>
        /// <returns>Success message</returns>
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> ActivateCurriculum(Guid id)
        {
            try
            {
                await _curriculumService.ActivateCurriculumAsync(id);
                return Ok(new { message = "Curriculum activated successfully", curriculumId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while activating the curriculum.", details = ex.Message });
            }
        }

        /// <summary>
        /// Deactivate a curriculum
        /// </summary>
        /// <param name="id">Curriculum ID</param>
        /// <returns>Success message</returns>
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DeactivateCurriculum(Guid id)
        {
            try
            {
                await _curriculumService.DeactivateCurriculumAsync(id);
                return Ok(new { message = "Curriculum deactivated successfully", curriculumId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deactivating the curriculum.", details = ex.Message });
            }
        }
    }
}