using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/centers")]
    [ApiController]
    public class CenterController : ControllerBase
    {
        private readonly ICenterService _centerService;
        private readonly ILocationService _locationService;
        public CenterController(ICenterService centerService, ILocationService locationService)
        {
            _centerService = centerService ?? throw new ArgumentNullException(nameof(centerService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        }

        /// <summary>
        /// Create a new learning center
        /// </summary>
        /// <param name="request">Center creation data</param>
        /// <returns>Created center ID</returns>
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> CreateCenter([FromBody] CreateCenterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var centerId = await _centerService.CreateCenterAsync(request);
                return CreatedAtAction(nameof(GetCenterById), new { id = centerId },
                    new { message = "Center created successfully", centerId });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("required"))
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("Google Maps"))
            {
                return BadRequest(new { error = "Invalid location. Please check the Place ID." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the center." });
            }
        }

        /// <summary>
        /// Update an existing center
        /// </summary>
        /// <param name="id">Center ID</param>
        /// <param name="request">Update data</param>
        /// <returns>No content</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateCenter(Guid id, [FromBody] UpdateCenterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _centerService.UpdateCenterAsync(id, request);
                return Ok(new { message = "Center updated successfully" });
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Center not found." });
            }
            catch (Exception ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("Google Maps"))
            {
                return BadRequest(new { error = "Invalid location. Please check the Place ID." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the center." });
            }
        }

        /// <summary>
        /// Delete a center (only if no active contracts/children)
        /// </summary>
        /// <param name="id">Center ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DeleteCenter(Guid id)
        {
            try
            {
                await _centerService.DeleteCenterAsync(id);
                return NoContent(); // 204 No Content for successful deletion
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Center not found." });
            }
            catch (Exception ex) when (ex.Message.Contains("Cannot delete"))
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the center." });
            }
        }

        /// <summary>
        /// Get center by ID
        /// </summary>
        /// <param name="id">Center ID</param>
        /// <returns>Center details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetCenterById(Guid id)
        {
            try
            {
                var center = await _centerService.GetCenterByIdAsync(id);
                return Ok(center);
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Center not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the center." });
            }
        }

        /// <summary>
        /// Get all centers with pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20)</param>
        /// <returns>List of centers</returns>
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetAllCenters([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var centers = await _centerService.GetAllCentersAsync();

                // Manual pagination since service doesn't support it yet
                var totalCount = centers.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var paginatedCenters = centers
                    .OrderBy(c => c.City)
                    .ThenBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    data = paginatedCenters,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        hasNext = page < totalPages,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving centers." });
            }
        }

        /// <summary>
        /// Get centers with their assigned tutors
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <returns>Centers with tutor details</returns>
        [HttpGet("with-tutors")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetCentersWithTutors([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var centersWithTutors = await _centerService.GetCentersWithTutorsAsync();

                // Manual pagination
                var totalCount = centersWithTutors.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var paginatedCenters = centersWithTutors
                    .OrderBy(c => c.City)
                    .ThenBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    data = paginatedCenters,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        hasNext = page < totalPages,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving centers with tutors." });
            }
        }

        /// <summary>
        /// Search centers by various criteria
        /// </summary>
        /// <param name="name">Center name (partial match)</param>
        /// <param name="city">City name (partial match)</param>
        /// <param name="district">District name (partial match)</param>
        /// <param name="latitude">Latitude for location-based search</param>
        /// <param name="longitude">Longitude for location-based search</param>
        /// <param name="radiusKm">Search radius in km (default: 10)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20)</param>
        /// <returns>Filtered centers</returns>
        [HttpGet("search")]
        [AllowAnonymous] // Allow public search for parents to find centers
        public async Task<IActionResult> SearchCenters(
            [FromQuery] string? name = null,
            [FromQuery] string? city = null,
            [FromQuery] string? district = null,
            [FromQuery][Range(-90, 90)] double? latitude = null,
            [FromQuery][Range(-180, 180)] double? longitude = null,
            [FromQuery][Range(0.1, 50)] double? radiusKm = 10.0,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination
                page = Math.Max(1, page);
                pageSize = Math.Min(50, Math.Max(1, pageSize)); // Max 50 per page

                var request = new CenterSearchRequest
                {
                    Name = name,
                    City = city,
                    District = district,
                    Latitude = latitude,
                    Longitude = longitude,
                    RadiusKm = radiusKm,
                    Page = page,
                    PageSize = pageSize
                };

                // Get paginated results
                var centers = await _centerService.SearchCentersAsync(request);

                // Get total count for accurate pagination
                var totalCount = await _centerService.GetCentersCountByCriteriaAsync(request);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return Ok(new
                {
                    data = centers,
                    searchCriteria = new
                    {
                        city = request.City,
                        district = request.District,
                        latitude = request.Latitude,
                        longitude = request.Longitude,
                        radiusKm = request.RadiusKm,
                        name = request.Name
                    },
                    pagination = new
                    {
                        currentPage = request.Page,
                        pageSize = request.PageSize,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        hasNext = request.Page < totalPages,
                        hasPrevious = request.Page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while searching for centers." });
            }
        }

        /// <summary>
        /// Get centers by city
        /// </summary>
        /// <param name="city">City name</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20)</param>
        /// <returns>Centers in the specified city</returns>
        [HttpGet("by-city/{city}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCentersByCity(string city, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                    return BadRequest(new { error = "City name is required." });

                var centers = await _centerService.GetCentersByCityAsync(city);

                // Manual pagination
                var totalCount = centers.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var paginatedCenters = centers
                    .OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    data = paginatedCenters,
                    city = city,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        hasNext = page < totalPages,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving centers by city." });
            }
        }

        /// <summary>
        /// Get centers near a specific location by address (Legacy endpoint - redirect to /api/location/nearby-centers)
        /// </summary>
        [HttpGet("near-location")]
        [AllowAnonymous]
        [Obsolete("This endpoint is deprecated. Use /api/location/nearby-centers instead.")]
        public async Task<IActionResult> GetCentersNearLocation(
            [FromQuery][Required] string address,
            [FromQuery][Range(0.1, 50)] double radiusKm = 10.0,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Redirect to the new unified endpoint
            return Redirect($"/api/location/nearby-centers?address={Uri.EscapeDataString(address)}&radiusKm={radiusKm}&page={page}&pageSize={pageSize}");
        }

        /// <summary>
        /// Assign a tutor to a center
        /// </summary>
        /// <param name="centerId">Center ID</param>
        /// <param name="request">Tutor assignment data</param>
        /// <returns>No content</returns>
        [HttpPost("{centerId}/assign-tutor")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> AssignTutorToCenter(Guid centerId, [FromBody] AssignTutorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _centerService.AssignTutorToCenterAsync(centerId, request.TutorId);
                return Ok(new { message = "Tutor assigned to center successfully" });
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Center or tutor not found." });
            }
            catch (Exception ex) when (ex.Message.Contains("already assigned"))
            {
                return Conflict(new { error = "Tutor is already assigned to this center." });
            }
            catch (Exception ex) when (ex.Message.Contains("verified"))
            {
                return BadRequest(new { error = "Tutor must be verified to be assigned to a center." });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { error = "An error occurred while assigning tutor to center." });
            }
        }

        /// <summary>
        /// Remove a tutor from a center
        /// </summary>
        /// <param name="centerId">Center ID</param>
        /// <param name="tutorId">Tutor ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{centerId}/remove-tutor/{tutorId}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> RemoveTutorFromCenter(Guid centerId, Guid tutorId)
        {
            try
            {
                await _centerService.RemoveTutorFromCenterAsync(centerId, tutorId);
                return Ok(new { message = "Tutor removed from center successfully" });
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Center or tutor not found." });
            }
            catch (Exception ex) when (ex.Message.Contains("not assigned"))
            {
                return BadRequest(new { error = "Tutor is not assigned to this center." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while removing tutor from center." });
            }
        }

        /// <summary>
        /// Get all tutors assigned to a center
        /// </summary>
        /// <param name="centerId">Center ID</param>
        /// <returns>List of tutors</returns>
        [HttpGet("{centerId}/tutors")]
        [Authorize(Roles = "admin,staff,parent")] // Parents can see tutors in their child's center
        public async Task<IActionResult> GetTutorsByCenterId(Guid centerId)
        {
            try
            {
                var tutors = await _centerService.GetTutorsByCenterIdAsync(centerId);
                return Ok(tutors);
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = "Center not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving tutors." });
            }
        }

        /// <summary>
        /// Get center statistics (for admin dashboard)
        /// </summary>
        /// <returns>Center statistics</returns>
        [HttpGet("statistics")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetCenterStatistics()
        {
            try
            {
                var totalCenters = await _centerService.GetAllCentersAsync();
                var centersWithTutors = await _centerService.GetCentersWithTutorsAsync();

                var stats = new
                {
                    totalCenters = totalCenters.Count,
                    centersWithTutors = centersWithTutors.Count(c => c.TutorCount > 0),
                    totalTutors = centersWithTutors.Sum(c => c.TutorCount),
                    avgTutorsPerCenter = totalCenters.Any() ? Math.Round((double)centersWithTutors.Sum(c => c.TutorCount) / totalCenters.Count, 1) : 0,
                    cities = totalCenters.Select(c => c.City).Where(c => !string.IsNullOrEmpty(c)).Distinct().Count(),
                    mostActiveCity = totalCenters
                        .GroupBy(c => c.City)
                        .Where(g => !string.IsNullOrEmpty(g.Key))
                        .OrderByDescending(g => g.Count())
                        .Select(g => new { City = g.Key, Count = g.Count() })
                        .FirstOrDefault() ?? new { City = "N/A", Count = 0 },
                    avgDistanceFromCenter = totalCenters.Any() ? Math.Round(totalCenters.Average(c => c.Latitude ?? 0), 2) : 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving center statistics." });
            }
        }
    }

    #region Request DTOs

    /// <summary>
    /// Request for assigning tutor to center
    /// </summary>
    public class AssignTutorRequest
    {
        [Required]
        public Guid TutorId { get; set; }
    }

    #endregion
}