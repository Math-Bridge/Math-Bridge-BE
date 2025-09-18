using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace MathBridgeSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "admin")] // Assuming admin-only access; adjust as needed
    public class SchoolsController : ControllerBase
    {
        private readonly ISchoolService _schoolService;

        public SchoolsController(ISchoolService schoolService)
        {
            _schoolService = schoolService ?? throw new ArgumentNullException(nameof(schoolService));
        }

        [HttpPost]
        public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest request)
        {
            try
            {
                var school = await _schoolService.CreateSchoolAsync(request);
                return Ok(school);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchoolById(Guid id)
        {
            try
            {
                var school = await _schoolService.GetSchoolByIdAsync(id);
                return Ok(school);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSchools()
        {
            var schools = await _schoolService.GetAllSchoolsAsync();
            return Ok(schools);
        }

/// <summary>
        /// Searches for schools within a specified radius (in km) from the current user's location.
        /// Requires authentication to retrieve the user's ID and location.
        /// </summary>
        /// <param name="radiusKm">The search radius in kilometers.</param>
        /// <returns>A list of schools within the radius.</returns>
        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> SearchSchools([FromQuery] double radiusKm)
        {
            try
            {
                var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userClaim == null || !Guid.TryParse(userClaim.Value, out Guid userId))
                {
                    return Unauthorized("User ID not found or invalid");
                }

                if (radiusKm <= 0)
                {
                    return BadRequest("Radius must be greater than 0");
                }

                var schools = await _schoolService.SearchByRadiusAsync(userId, radiusKm);
                return Ok(schools);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }        
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchool(Guid id, [FromBody] UpdateSchoolRequest request)
        {
            try
            {
                var school = await _schoolService.UpdateSchoolAsync(id, request);
                return Ok(new {school});
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchool(Guid id)
        {
            try
            {
                await _schoolService.DeleteSchoolAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}