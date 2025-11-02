using MathBridgeSystem.Application.DTOs.School;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/schools")]
    [ApiController]
    public class SchoolController : ControllerBase
    {
        private readonly ISchoolService _schoolService;

        public SchoolController(ISchoolService schoolService)
        {
            _schoolService = schoolService ?? throw new ArgumentNullException(nameof(schoolService));
        }

        /// <summary>
        /// Create a new school
        /// </summary>
        /// <param name="request">School creation data</param>
        /// <returns>Created school ID</returns>
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var schoolId = await _schoolService.CreateSchoolAsync(request);
                return CreatedAtAction(nameof(GetSchoolById), new { id = schoolId },
                    new { message = "School created successfully", schoolId });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("not found"))
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
                return StatusCode(500, new { error = "An error occurred while creating the school.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing school
        /// </summary>
        /// <param name="id">School ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Success message</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateSchool(Guid id, [FromBody] UpdateSchoolRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _schoolService.UpdateSchoolAsync(id, request);
                return Ok(new { message = "School updated successfully", schoolId = id });
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
                return StatusCode(500, new { error = "An error occurred while updating the school.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a school (only if no enrolled children)
        /// </summary>
        /// <param name="id">School ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DeleteSchool(Guid id)
        {
            try
            {
                await _schoolService.DeleteSchoolAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("children"))
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the school.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get school by ID
        /// </summary>
        /// <param name="id">School ID</param>
        /// <returns>School details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetSchoolById(Guid id)
        {
            try
            {
                var school = await _schoolService.GetSchoolByIdAsync(id);
                if (school == null)
                    return NotFound(new { error = "School not found." });

                return Ok(school);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the school.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all schools with pagination
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of schools with pagination</returns>
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetAllSchools([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var schools = await _schoolService.GetAllSchoolsAsync();
                var totalCount = schools.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                
                var pagedSchools = schools
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    data = pagedSchools,
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
                return StatusCode(500, new { error = "An error occurred while retrieving schools.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get active schools (public access)
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of active schools</returns>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveSchools([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var schools = await _schoolService.GetActiveSchoolsAsync();
                var totalCount = schools.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                
                var pagedSchools = schools
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    data = pagedSchools,
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
                return StatusCode(500, new { error = "An error occurred while retrieving active schools.", details = ex.Message });
            }
        }

        /// <summary>
        /// Search schools with filters and pagination
        /// </summary>
        /// <param name="request">Search criteria</param>
        /// <returns>Filtered list of schools</returns>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchSchools([FromQuery] SchoolSearchRequest request)
        {
            try
            {
                var schools = await _schoolService.SearchSchoolsAsync(request);
                var totalCount = await _schoolService.GetSchoolsCountAsync(request);
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

                return Ok(new
                {
                    data = schools,
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
                return StatusCode(500, new { error = "An error occurred while searching schools.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get schools by curriculum ID
        /// </summary>
        /// <param name="curriculumId">Curriculum ID</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of schools for the curriculum</returns>
        [HttpGet("by-curriculum/{curriculumId}")]
        [Authorize]
        public async Task<IActionResult> GetSchoolsByCurriculum(Guid curriculumId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var schools = await _schoolService.GetSchoolsByCurriculumAsync(curriculumId);
                var totalCount = schools.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                
                var pagedSchools = schools
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    data = pagedSchools,
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
                return StatusCode(500, new { error = "An error occurred while retrieving schools by curriculum.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get children enrolled in a school
        /// </summary>
        /// <param name="id">School ID</param>
        /// <returns>List of children</returns>
        [HttpGet("{id}/children")]
        [Authorize(Roles = "admin,staff,tutor")]
        public async Task<IActionResult> GetChildrenBySchool(Guid id)
        {
            try
            {
                var children = await _schoolService.GetChildrenBySchoolAsync(id);
                return Ok(children);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving children.", details = ex.Message });
            }
        }

        /// <summary>
        /// Activate a school
        /// </summary>
        /// <param name="id">School ID</param>
        /// <returns>Success message</returns>
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> ActivateSchool(Guid id)
        {
            try
            {
                await _schoolService.ActivateSchoolAsync(id);
                return Ok(new { message = "School activated successfully", schoolId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while activating the school.", details = ex.Message });
            }
        }

        /// <summary>
        /// Deactivate a school
        /// </summary>
        /// <param name="id">School ID</param>
        /// <returns>Success message</returns>
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DeactivateSchool(Guid id)
        {
            try
            {
                await _schoolService.DeactivateSchoolAsync(id);
                return Ok(new { message = "School deactivated successfully", schoolId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deactivating the school.", details = ex.Message });
            }
        }
    }
}