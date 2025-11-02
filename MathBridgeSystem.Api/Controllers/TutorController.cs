using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TutorsController : ControllerBase
    {
        private readonly ITutorService _tutorService;

        public TutorsController(ITutorService tutorService)
        {
            _tutorService = tutorService ?? throw new ArgumentNullException(nameof(tutorService));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<TutorDto>> GetTutor(Guid id)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var tutor = await _tutorService.GetTutorByIdAsync(id, currentUserId, currentUserRole);
                return Ok(tutor);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<ActionResult<Guid>> UpdateTutor(Guid id, [FromBody] UpdateTutorRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body cannot be empty" });

                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var result = await _tutorService.UpdateTutorAsync(id, request, currentUserId, currentUserRole);
                return Ok(new { tutorId = result, message = "Tutor updated successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
