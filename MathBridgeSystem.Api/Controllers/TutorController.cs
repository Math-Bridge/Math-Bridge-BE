using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/tutors")]
    [ApiController]
    [Authorize]
    public class TutorController : ControllerBase
    {
        private readonly ITutorService _tutorService;

        public TutorController(ITutorService tutorService)
        {
            _tutorService = tutorService ?? throw new ArgumentNullException(nameof(tutorService));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTutor(Guid id)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("Invalid token"));
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(currentUserRole))
                    return Unauthorized(new { error = "Role not found in token" });

                var tutor = await _tutorService.GetTutorByIdAsync(id, currentUserId, currentUserRole);
                return Ok(tutor);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTutor: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error while getting tutor" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTutor(Guid id, [FromBody] UpdateTutorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("Invalid token"));
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(currentUserRole))
                    return Unauthorized(new { error = "Role not found in token" });

                var tutorId = await _tutorService.UpdateTutorAsync(id, request, currentUserId, currentUserRole);
                return Ok(new { tutorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateTutor: {ex.ToString()}");

                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error while updating tutor" : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}
